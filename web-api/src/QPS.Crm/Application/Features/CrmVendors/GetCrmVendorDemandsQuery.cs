using QPS.Shared.Common;
using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.CrmVendors;
using QPS.Shared.Extensions;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Exceptions;
using QPS.Crm.Application.Features.CrmMatchOpportunities;

namespace QPS.Crm.Application.Features.CrmVendors;

public class GetCrmVendorDemandsQuery : PaginationRequest, IRequest<PaginationResponse<CrmVendorDemandDto>>
{
    public Guid? Id { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? Status { get; set; }
    public string? ProductName { get; set; }
    public string? Keyword { get; set; }
    /// <summary>撮合状态筛选：空闲 / 撮合中 / 成交。</summary>
    public string? MatchStatus { get; set; }
    public DateTime? DemandAtFrom { get; set; }
    public DateTime? DemandAtTo { get; set; }
    public DateTime? ExpectedDeliveryAtFrom { get; set; }
    public DateTime? ExpectedDeliveryAtTo { get; set; }
}
public class GetCrmVendorDemandsHandler : IRequestHandler<GetCrmVendorDemandsQuery, PaginationResponse<CrmVendorDemandDto>>
{
    private readonly ICrmDbContext _dbContext;

    public GetCrmVendorDemandsHandler(ICrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<CrmVendorDemandDto>> Handle(
        GetCrmVendorDemandsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.VendorId.HasValue && !await _dbContext.CrmVendors.AnyAsync(vendor => vendor.Id == request.VendorId, cancellationToken))
        {
            throw new BusinessException(404, "厂商不存在");
        }

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.CrmVendorDemands.AsQueryable();
        if (request.Id.HasValue) query = query.Where(plan => plan.Id == request.Id);
        if (request.VendorId.HasValue) query = query.Where(plan => plan.VendorId == request.VendorId);
        if (request.OwnerUserId.HasValue) query = query.Where(plan => plan.Vendor!.OwnerUserId == request.OwnerUserId);
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(plan => plan.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.ProductName)) query = query.Where(plan => plan.Items.Any(item => item.ProductName == request.ProductName));
        if (!string.IsNullOrWhiteSpace(request.Keyword)) query = query.Where(plan => plan.DemandNo.Contains(request.Keyword) || plan.DemandName.Contains(request.Keyword) || plan.Vendor!.VendorName.Contains(request.Keyword));
        if (request.DemandAtFrom.HasValue) query = query.Where(plan => plan.DemandAt >= request.DemandAtFrom);
        if (request.DemandAtTo.HasValue) query = query.Where(plan => plan.DemandAt <= request.DemandAtTo);
        if (request.ExpectedDeliveryAtFrom.HasValue) query = query.Where(plan => plan.ExpectedDeliveryAt >= request.ExpectedDeliveryAtFrom);
        if (request.ExpectedDeliveryAtTo.HasValue) query = query.Where(plan => plan.ExpectedDeliveryAt <= request.ExpectedDeliveryAtTo);
        if (request.MatchStatus is "成交" or "撮合中" or "空闲")
        {
            var occupiedItemIds = CrmMatchOpportunitySource.OccupiedDemandItemIds(_dbContext);
            var matchedItemIds = CrmMatchOpportunitySource.MatchedDemandItemIds(_dbContext);
            query = request.MatchStatus switch
            {
                "成交" => query.Where(plan => plan.Items.Any(item => matchedItemIds.Contains(item.Id))),
                "撮合中" => query.Where(plan => plan.Items.Any(item =>
                    occupiedItemIds.Contains(item.Id) && !matchedItemIds.Contains(item.Id))),
                _ => query.Where(plan => !plan.Items.Any(item => occupiedItemIds.Contains(item.Id)) &&
                    !plan.Items.Any(item => matchedItemIds.Contains(item.Id)))
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var isAscending = string.Equals(request.SortDirection, "Ascending", StringComparison.OrdinalIgnoreCase);
        var orderedQuery = request.SortField switch
        {
            nameof(CrmVendorDemandDto.DemandName) => isAscending
                ? query.OrderBy(plan => plan.DemandName).ThenByDescending(plan => plan.CreatedAt)
                : query.OrderByDescending(plan => plan.DemandName).ThenByDescending(plan => plan.CreatedAt),
            nameof(CrmVendorDemandDto.CreatedAt) => isAscending
                ? query.OrderBy(plan => plan.CreatedAt)
                : query.OrderByDescending(plan => plan.CreatedAt),
            nameof(CrmVendorDemandDto.UpdatedAt) => isAscending
                ? query.OrderBy(plan => plan.UpdatedAt)
                : query.OrderByDescending(plan => plan.UpdatedAt),
            _ => isAscending
                ? query.OrderBy(plan => plan.DemandAt).ThenBy(plan => plan.CreatedAt)
                : query.OrderByDescending(plan => plan.DemandAt).ThenByDescending(plan => plan.CreatedAt)
        };

        var demandIds = await orderedQuery
            .Select(plan => plan.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var plans = await _dbContext.CrmVendorDemands
            .Where(plan => demandIds.Contains(plan.Id))
            .Select(plan => new CrmVendorDemandDto
            {
                Id = plan.Id,
                VendorId = plan.VendorId,
                DemandNo = plan.DemandNo,
                DemandName = plan.DemandName,
                DemandAt = plan.DemandAt,
                Status = plan.Status,
                SourceType = plan.SourceType,
                ContactId = plan.ContactId,
                ContactName = string.Empty,
                OwnerUserName = string.Empty,
                ExpectedDeliveryAt = plan.ExpectedDeliveryAt,
                ReceivingAddress = plan.ReceivingAddress,
                SourceUrl = plan.SourceUrl,
                Remark = plan.Remark,
                ClosedReason = plan.ClosedReason,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        plans = demandIds
            .Select(id => plans.First(plan => plan.Id == id))
            .ToList();

        var planIds = plans.Select(plan => plan.Id).ToList();
        var items = await _dbContext.CrmVendorDemandItems
            .Where(item => planIds.Contains(item.VendorDemandId))
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);

        var itemIds = items.Select(item => item.Id).ToList();
        var matchContext = await CrmVendorDemands.LoadItemMatchContextAsync(_dbContext, itemIds, cancellationToken);

        foreach (var plan in plans)
        {
            plan.Items = items
                .Where(item => item.VendorDemandId == plan.Id)
                .Select(item => CrmVendorDemands.ToItemDto(item, matchContext))
                .ToList();
            plan.MatchProgress = CrmVendorDemands.BuildMatchProgress(plan.Items);
        }

        return new PaginationResponse<CrmVendorDemandDto>(plans, totalCount, page, pageSize);
    }
}