using QPS.Shared.Common;
using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Application.Features.CrmMatchOpportunities;
using QPS.Crm.Contracts.DTOs.CrmVendors;
using QPS.Crm.Contracts.Permissions;
using QPS.Shared.Contracts;

namespace QPS.Crm.Application.Features.CrmVendors;

/// <summary>采购列表按采购明细分页：一条明细一行，单据级字段随行返回供前端合并展示。</summary>
[Permission(CrmPermissionCodes.CRM_PURCHASE_DEMAND_VIEW)]
public class GetCrmVendorDemandItemsQuery : PaginationRequest, IRequest<PaginationResponse<CrmVendorDemandItemRowDto>>
{
    public Guid? DemandId { get; set; }
    public Guid? VendorId { get; set; }
    public string? Status { get; set; }
    public string? Keyword { get; set; }
    /// <summary>撮合状态筛选：空闲 / 撮合中 / 成交。</summary>
    public string? MatchStatus { get; set; }
    public DateTime? DemandAtFrom { get; set; }
    public DateTime? DemandAtTo { get; set; }
    public DateTime? ExpectedDeliveryAtFrom { get; set; }
    public DateTime? ExpectedDeliveryAtTo { get; set; }
}

public class GetCrmVendorDemandItemsHandler : IRequestHandler<GetCrmVendorDemandItemsQuery, PaginationResponse<CrmVendorDemandItemRowDto>>
{
    private readonly ICrmDbContext _dbContext;

    public GetCrmVendorDemandItemsHandler(ICrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<CrmVendorDemandItemRowDto>> Handle(
        GetCrmVendorDemandItemsQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.CrmVendorDemandItems.AsQueryable();
        if (request.DemandId.HasValue) query = query.Where(item => item.VendorDemandId == request.DemandId);
        if (request.VendorId.HasValue) query = query.Where(item => item.VendorDemand!.VendorId == request.VendorId);
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(item => item.VendorDemand!.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(item =>
                item.ProductName.Contains(keyword) ||
                item.VendorDemand!.DemandNo.Contains(keyword) ||
                item.VendorDemand.DemandName.Contains(keyword) ||
                item.VendorDemand.Vendor!.VendorName.Contains(keyword));
        }
        if (request.DemandAtFrom.HasValue) query = query.Where(item => item.VendorDemand!.DemandAt >= request.DemandAtFrom);
        if (request.DemandAtTo.HasValue) query = query.Where(item => item.VendorDemand!.DemandAt <= request.DemandAtTo);
        if (request.ExpectedDeliveryAtFrom.HasValue)
        {
            query = query.Where(item => (item.ExpectedDeliveryAt ?? item.VendorDemand!.ExpectedDeliveryAt) >= request.ExpectedDeliveryAtFrom);
        }
        if (request.ExpectedDeliveryAtTo.HasValue)
        {
            query = query.Where(item => (item.ExpectedDeliveryAt ?? item.VendorDemand!.ExpectedDeliveryAt) <= request.ExpectedDeliveryAtTo);
        }
        if (request.MatchStatus is "成交" or "撮合中" or "空闲")
        {
            var occupiedItemIds = CrmMatchOpportunitySource.OccupiedDemandItemIds(_dbContext);
            var matchedItemIds = CrmMatchOpportunitySource.MatchedDemandItemIds(_dbContext);
            query = request.MatchStatus switch
            {
                "成交" => query.Where(item => matchedItemIds.Contains(item.Id)),
                "撮合中" => query.Where(item => occupiedItemIds.Contains(item.Id) && !matchedItemIds.Contains(item.Id)),
                _ => query.Where(item => !occupiedItemIds.Contains(item.Id) && !matchedItemIds.Contains(item.Id))
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // 采购单维度的连续行在页面上合并单元格，与供应列表一致：排序按单据归组，近期更新的单据优先
        var pageItemIds = await query
            .OrderByDescending(item => item.VendorDemand!.UpdatedAt)
            .ThenBy(item => item.VendorDemand!.DemandName)
            .ThenBy(item => item.VendorDemand!.DemandNo)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        if (pageItemIds.Count == 0)
        {
            return new PaginationResponse<CrmVendorDemandItemRowDto>(new(), totalCount, page, pageSize);
        }

        var allItems = await _dbContext.CrmVendorDemandItems
            .Where(item => pageItemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        var demandIds = allItems.Select(item => item.VendorDemandId).Distinct().ToList();
        var demands = await _dbContext.CrmVendorDemands
            .Where(demand => demandIds.Contains(demand.Id))
            .ToListAsync(cancellationToken);

        // 单据撮合进度按该单据的全部明细计算，不受当前分页影响。
        var demandItems = await _dbContext.CrmVendorDemandItems
            .Where(item => demandIds.Contains(item.VendorDemandId))
            .ToListAsync(cancellationToken);
        var matchContext = await CrmVendorDemands.LoadItemMatchContextAsync(
            _dbContext,
            demandItems.Select(item => item.Id).ToList(),
            cancellationToken);

        var itemDtoById = demandItems.ToDictionary(
            item => item.Id,
            item => CrmVendorDemands.ToItemDto(item, matchContext));
        var progressByDemandId = demandItems
            .GroupBy(item => item.VendorDemandId)
            .ToDictionary(
                group => group.Key,
                group => CrmVendorDemands.BuildMatchProgress(group.Select(item => itemDtoById[item.Id])));

        var itemById = allItems.ToDictionary(item => item.Id);
        var demandById = demands.ToDictionary(demand => demand.Id);
        var rows = pageItemIds
            .Select(itemId =>
            {
                var item = itemById[itemId];
                var demand = demandById[item.VendorDemandId];
                return new CrmVendorDemandItemRowDto
                {
                    Item = itemDtoById[itemId],
                    Demand = new CrmVendorDemandBriefDto
                    {
                        Id = demand.Id,
                        VendorId = demand.VendorId,
                        DemandNo = demand.DemandNo,
                        DemandName = demand.DemandName,
                        DemandAt = demand.DemandAt,
                        Status = demand.Status,
                        SourceType = demand.SourceType,
                        ExpectedDeliveryAt = demand.ExpectedDeliveryAt,
                        ReceivingAddress = demand.ReceivingAddress,
                        MatchProgress = progressByDemandId[demand.Id]
                    }
                };
            })
            .ToList();

        return new PaginationResponse<CrmVendorDemandItemRowDto>(rows, totalCount, page, pageSize);
    }
}
