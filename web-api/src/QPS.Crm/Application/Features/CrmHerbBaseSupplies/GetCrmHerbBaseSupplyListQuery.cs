using QPS.Shared.Common;
using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs.CrmHerbBaseSupplies;
using QPS.Shared.Extensions;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Crm.Application.Features.CrmMatchOpportunities;

namespace QPS.Crm.Application.Features.CrmHerbBaseSupplies;

public class GetCrmHerbBaseSupplyListQuery : PaginationRequest, IRequest<PaginationResponse<CrmHerbBaseSupplyListDto>>
{
    public string ProductName { get; set; } = string.Empty;
    public string HerbBaseName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    /// <summary>撮合状态筛选：空闲 / 撮合中 / 成交。</summary>
    public string MatchStatus { get; set; } = string.Empty;
}

public class GetCrmHerbBaseSupplyListHandler(ICrmDbContext dbContext)
    : IRequestHandler<GetCrmHerbBaseSupplyListQuery, PaginationResponse<CrmHerbBaseSupplyListDto>>
{
    public async Task<PaginationResponse<CrmHerbBaseSupplyListDto>> Handle(
        GetCrmHerbBaseSupplyListQuery request,
        CancellationToken cancellationToken)
    {
        var occupiedSupplyIds = CrmMatchOpportunitySource.OccupiedEntityIds(dbContext, CrmMatchOpportunityItem.EntityTypeSupply);
        var matchedSupplyIds = CrmMatchOpportunitySource.MatchedSupplyIds(dbContext);

        var supplies = dbContext.CrmHerbBaseSupplies
            .Include(supply => supply.HerbBase)
            .Where(supply => supply.HerbBase != null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ProductName))
        {
            var productName = request.ProductName.Trim();
            supplies = supplies.Where(supply => supply.ProductName.Contains(productName));
        }

        if (!string.IsNullOrWhiteSpace(request.HerbBaseName))
        {
            var herbBaseName = request.HerbBaseName.Trim();
            supplies = supplies.Where(supply => supply.HerbBase!.BaseName.Contains(herbBaseName) ||
                supply.HerbBase.SubjectName.Contains(herbBaseName));
        }

        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            var region = request.Region.Trim();
            supplies = supplies.Where(supply => supply.HerbBase!.Province.Contains(region) ||
                supply.HerbBase.City.Contains(region) || supply.HerbBase.Area.Contains(region));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            supplies = supplies.Where(supply => supply.Status == request.Status.Trim());
        }

        if (request.MatchStatus == "成交")
        {
            supplies = supplies.Where(supply => matchedSupplyIds.Contains(supply.Id));
        }
        else if (request.MatchStatus == "撮合中")
        {
            supplies = supplies.Where(supply => occupiedSupplyIds.Contains(supply.Id) && !matchedSupplyIds.Contains(supply.Id));
        }
        else if (request.MatchStatus == "空闲")
        {
            supplies = supplies.Where(supply => !occupiedSupplyIds.Contains(supply.Id) && !matchedSupplyIds.Contains(supply.Id));
        }

        var totalCount = await supplies.CountAsync(cancellationToken);
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Max(request.PageSize, 1);
        var list = await supplies
            // 基地维度的连续行在页面上合并单元格，排序需按基地归组；近期更新的基地优先
            .OrderByDescending(supply => supply.HerbBase!.UpdatedAt)
            .ThenBy(supply => supply.HerbBase!.BaseName)
            .ThenBy(supply => supply.ProductName)
            .ThenByDescending(supply => supply.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(supply => new CrmHerbBaseSupplyListDto
            {
                Id = supply.Id,
                HerbBaseId = supply.HerbBaseId,
                HerbBaseSubjectId = supply.HerbBaseSubjectId,
                SubjectName = supply.HerbBase!.SubjectName,
                HerbBaseName = supply.HerbBase.BaseName,
                Province = supply.HerbBase.Province,
                City = supply.HerbBase.City,
                Area = supply.HerbBase.Area,
                ProductName = supply.ProductName,
                AvailableQuantity = supply.AvailableQuantity,
                QuantityUnit = supply.QuantityUnit,
                Specification = supply.Specification,
                QualityRequirement = supply.QualityRequirement,
                HarvestSeason = supply.HarvestSeason,
                ExpectedPrice = supply.ExpectedPrice,
                PriceUnit = supply.PriceUnit,
                SupplyCycle = supply.SupplyCycle,
                ConfirmedAt = supply.ConfirmedAt,
                ValidUntil = supply.ValidUntil,
                Status = supply.Status,
                Remark = supply.Remark,
                IsExpired = supply.Status == CrmHerbBaseSupply.Active && supply.ValidUntil < DateTime.Today,
                CandidateCount = dbContext.CrmMatchOpportunityItems.Count(item =>
                    item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply && item.EntityId == supply.Id),
                MatchStatus = matchedSupplyIds.Contains(supply.Id)
                    ? "成交"
                    : occupiedSupplyIds.Contains(supply.Id) ? "撮合中" : "空闲",
                SelectedCount = dbContext.CrmMatchOpportunityItems.Count(item =>
                    item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                    item.EntityId == supply.Id &&
                    item.Status == CrmMatchOpportunityItem.Selected &&
                    dbContext.CrmMatchOpportunities.Any(opportunity =>
                        opportunity.Id == item.OpportunityId &&
                        opportunity.Status != CrmMatchOpportunity.Closed))
            })
            .ToListAsync(cancellationToken);

        var supplyIds = list.Select(item => item.Id).ToList();

        // 最近一次参与的撮合机会 ID（含已关闭的成交/结束记录），供列表查看历史详情。
        // 供应无论发起方还是候选方，都会作为 supply 类型 item 出现在子表中。
        // 优先取中选记录所在机会（能看到成交候选），否则按机会创建时间取最新一条。
        var latestOpportunities = await (
                from item in dbContext.CrmMatchOpportunityItems.AsNoTracking()
                join opportunity in dbContext.CrmMatchOpportunities.AsNoTracking() on item.OpportunityId equals opportunity.Id
                where item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply && supplyIds.Contains(item.EntityId)
                select new
                {
                    item.EntityId,
                    OpportunityId = opportunity.Id,
                    item.Status,
                    opportunity.CreatedAt
                }
            )
            .ToListAsync(cancellationToken);
        var latestOpportunityBySupply = latestOpportunities
            .GroupBy(entry => entry.EntityId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(entry => entry.Status == CrmMatchOpportunityItem.Selected)
                    .ThenByDescending(entry => entry.CreatedAt)
                    .First()
                    .OpportunityId);

        // 供应被哪些未关闭机会占用，并取这些机会里需求发起方明细的品类（实时取原明细）。
        var occupiedDemands = await (
                from item in dbContext.CrmMatchOpportunityItems.AsNoTracking()
                join opportunity in dbContext.CrmMatchOpportunities.AsNoTracking() on item.OpportunityId equals opportunity.Id
                join demandItemRow in dbContext.CrmMatchOpportunityItems.AsNoTracking()
                    on opportunity.Id equals demandItemRow.OpportunityId
                join demandItem in dbContext.CrmVendorDemandItems.AsNoTracking()
                    on demandItemRow.EntityId equals demandItem.Id into demandGroup
                from demandItem in demandGroup.DefaultIfEmpty()
                where item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                    supplyIds.Contains(item.EntityId) &&
                    item.Status != CrmMatchOpportunityItem.Eliminated &&
                    opportunity.Status != CrmMatchOpportunity.Closed &&
                    demandItemRow.EntityType == CrmMatchOpportunityItem.EntityTypeDemand
                select new
                {
                    OpportunityId = opportunity.Id,
                    item.EntityId,
                    ProductName = demandItem == null ? string.Empty : demandItem.ProductName
                })
            .ToListAsync(cancellationToken);

        foreach (var item in list)
        {
            var itemDemands = occupiedDemands.Where(demand => demand.EntityId == item.Id).ToList();
            item.OccupiedDemandSummary = string.Join("、", itemDemands
                .Select(demand => demand.ProductName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct());
            item.OccupiedOpportunityIds = itemDemands
                .Select(demand => demand.OpportunityId)
                .Distinct()
                .ToList();
            item.LatestOpportunityId = latestOpportunityBySupply.GetValueOrDefault(item.Id);
        }

        return new PaginationResponse<CrmHerbBaseSupplyListDto>(list, totalCount, page, pageSize);
    }
}
