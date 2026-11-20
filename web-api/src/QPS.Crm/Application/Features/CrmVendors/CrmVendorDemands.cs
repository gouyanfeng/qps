using QPS.Crm.Contracts;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Application.Features.CrmMatchOpportunities;
using QPS.Crm.Contracts.DTOs.CrmVendors;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmVendors;

internal static class CrmVendorDemands
{
    /// <summary>采购明细的撮合聚合结果：最新机会、候选数、已采购数量、占用关系。</summary>
    public sealed class DemandItemMatchContext
    {
        public Dictionary<Guid, MatchOpportunitySummary> LatestOpportunityByItemId { get; init; } = new();
        public Dictionary<Guid, int> CandidateCounts { get; init; } = new();
        public Dictionary<Guid, decimal> MatchedQuantities { get; init; } = new();
        public Dictionary<Guid, List<Guid>> OccupancyByItemId { get; init; } = new();
        public HashSet<Guid> MatchedItemIds { get; init; } = new();
    }

    public sealed class MatchOpportunitySummary
    {
        public Guid Id { get; init; }
        public Guid DemandItemId { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        /// <summary>该机会是否含中选记录（详情优先打开这类机会，确保能看到中选候选）。</summary>
        public bool HasSelected { get; set; }
    }

    public static async Task<DemandItemMatchContext> LoadItemMatchContextAsync(
        ICrmDbContext dbContext,
        IReadOnlyCollection<Guid> demandItemIds,
        CancellationToken cancellationToken)
    {
        var itemIds = demandItemIds.Distinct().ToList();
        if (itemIds.Count == 0)
        {
            return new DemandItemMatchContext();
        }

        var occupancyByItemId = await CrmMatchOpportunitySource.LoadDemandOccupancyAsync(dbContext, itemIds, cancellationToken);
        var matchedItemIdSet = (await CrmMatchOpportunitySource.MatchedDemandItemIds(dbContext)
            .Distinct()
            .ToListAsync(cancellationToken))
            .ToHashSet();

        // 明细关联的机会：需求明细作为子表 item 出现，一条明细可能关联多个机会。
        var opportunityRows = await (
                from item in dbContext.CrmMatchOpportunityItems.AsNoTracking()
                join opportunity in dbContext.CrmMatchOpportunities.AsNoTracking() on item.OpportunityId equals opportunity.Id
                where item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                    itemIds.Contains(item.EntityId)
                select new
                {
                    opportunity.Id,
                    DemandItemId = item.EntityId,
                    opportunity.Status,
                    opportunity.CreatedAt
                })
            .ToListAsync(cancellationToken);

        // 含中选记录的机会：同机会里 supply 侧中选（采购发起），或 demand 侧中选（供应发起）。
        var selectedOpportunityIds = (await dbContext.CrmMatchOpportunityItems.AsNoTracking()
                .Where(item => item.Status == CrmMatchOpportunityItem.Selected)
                .Select(item => item.OpportunityId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var allOpportunities = opportunityRows.Select(row => new MatchOpportunitySummary
        {
            Id = row.Id,
            DemandItemId = row.DemandItemId,
            Status = row.Status,
            CreatedAt = row.CreatedAt
        }).ToList();
        foreach (var opportunity in allOpportunities)
        {
            opportunity.HasSelected = selectedOpportunityIds.Contains(opportunity.Id);
        }
        // 优先取有中选记录的机会；同为有/无中选时，再按创建时间倒序取最新一条。
        var latestOpportunityByItemId = allOpportunities
            .OrderByDescending(opportunity => opportunity.HasSelected)
            .ThenByDescending(opportunity => opportunity.CreatedAt)
            .GroupBy(opportunity => opportunity.DemandItemId)
            .ToDictionary(group => group.Key, group => group.First());
        var latestOpportunityIds = latestOpportunityByItemId.Values.Select(opportunity => opportunity.Id).ToList();
        var candidateCounts = await dbContext.CrmMatchOpportunityItems
            .Where(item => latestOpportunityIds.Contains(item.OpportunityId) && item.Role == CrmMatchOpportunityItem.Candidate)
            .GroupBy(item => item.OpportunityId)
            .Select(group => new { OpportunityId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.OpportunityId, item => item.Count, cancellationToken);

        // 已中选数量按「撮合成功关闭机会中的中选记录关联的原明细当前数量」累计：
        // demand 来源（采购发起）→ 中选供应明细的可供量；supply 来源（供应发起）→ 中选需求明细的数量。
        // 原明细成交后冻结不可改，故当前数量即成交数量，稳定可审计。
        var closedMatched = await (
                from item in dbContext.CrmMatchOpportunityItems.AsNoTracking()
                join opportunity in dbContext.CrmMatchOpportunities.AsNoTracking() on item.OpportunityId equals opportunity.Id
                where item.Status == CrmMatchOpportunityItem.Selected &&
                    opportunity.Status == CrmMatchOpportunity.Closed &&
                    opportunity.CloseResult == CrmMatchOpportunity.Matched
                select new
                {
                    opportunity.Source,
                    item.EntityType,
                    item.EntityId
                })
            .ToListAsync(cancellationToken);

        var selectedSupplyIds = closedMatched
            .Where(x => x.Source == "demand" && x.EntityType == CrmMatchOpportunityItem.EntityTypeSupply)
            .Select(x => x.EntityId).Distinct().ToList();
        var selectedDemandIds = closedMatched
            .Where(x => x.Source == "supply" && x.EntityType == CrmMatchOpportunityItem.EntityTypeDemand)
            .Select(x => x.EntityId).Distinct().ToList();

        // 采购发起：中选的是供应明细，需通过 demand 来源机会把供应量归到对应采购明细。
        var matchedSupplyByDemandItem = await (
                from opportunity in dbContext.CrmMatchOpportunities.AsNoTracking()
                join demandItemRow in dbContext.CrmMatchOpportunityItems.AsNoTracking()
                    on opportunity.Id equals demandItemRow.OpportunityId
                join selectedSupply in dbContext.CrmMatchOpportunityItems.AsNoTracking()
                    on opportunity.Id equals selectedSupply.OpportunityId
                join supply in dbContext.CrmHerbBaseSupplies.AsNoTracking().IgnoreQueryFilters()
                    on selectedSupply.EntityId equals supply.Id
                where opportunity.Source == "demand" &&
                    opportunity.Status == CrmMatchOpportunity.Closed &&
                    opportunity.CloseResult == CrmMatchOpportunity.Matched &&
                    demandItemRow.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                    demandItemRow.Role == CrmMatchOpportunityItem.Initiator &&
                    selectedSupply.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                    selectedSupply.Status == CrmMatchOpportunityItem.Selected &&
                    selectedSupplyIds.Contains(supply.Id) &&
                    itemIds.Contains(demandItemRow.EntityId)
                group supply by demandItemRow.EntityId into g
                select new { DemandItemId = g.Key, Quantity = g.Sum(s => s.AvailableQuantity ?? 0m) })
            .ToDictionaryAsync(x => x.DemandItemId, x => x.Quantity, cancellationToken);

        // 供应发起：中选的是采购明细，数量直接取该采购明细当前数量。
        var matchedDemandQuantities = await dbContext.CrmVendorDemandItems.AsNoTracking()
            .Where(item => selectedDemandIds.Contains(item.Id) && itemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Quantity ?? 0m, cancellationToken);

        var matchedQuantities = new Dictionary<Guid, decimal>();
        foreach (var itemId in itemIds)
        {
            var qty = matchedSupplyByDemandItem.GetValueOrDefault(itemId) + matchedDemandQuantities.GetValueOrDefault(itemId);
            if (qty > 0) matchedQuantities[itemId] = qty;
        }

        return new DemandItemMatchContext
        {
            LatestOpportunityByItemId = latestOpportunityByItemId,
            CandidateCounts = candidateCounts,
            MatchedQuantities = matchedQuantities,
            OccupancyByItemId = occupancyByItemId,
            MatchedItemIds = matchedItemIdSet
        };
    }

    public static CrmVendorDemandItemDto ToItemDto(CrmVendorDemandItem item, DemandItemMatchContext context)
    {
        context.LatestOpportunityByItemId.TryGetValue(item.Id, out var latestOpportunity);
        var status = latestOpportunity?.Status ?? string.Empty;
        var matchedQuantity = context.MatchedQuantities.GetValueOrDefault(item.Id);
        var hasQuantity = item.Quantity.HasValue && item.Quantity.Value > 0;
        var remainingQuantity = hasQuantity ? Math.Max(0m, item.Quantity!.Value - matchedQuantity) : 0m;
        var occupiedBy = context.OccupancyByItemId.TryGetValue(item.Id, out var opportunityIds) ? opportunityIds : new List<Guid>();
        return new CrmVendorDemandItemDto
        {
            Id = item.Id,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            QuantityUnit = item.QuantityUnit,
            Specification = item.Specification,
            QualityRequirement = item.QualityRequirement,
            TargetPrice = item.TargetPrice,
            PriceUnit = item.PriceUnit,
            ExpectedDeliveryAt = item.ExpectedDeliveryAt,
            Remark = item.Remark,
            SortOrder = item.SortOrder,
            MatchOpportunityId = latestOpportunity?.Id,
            MatchOpportunityStatus = status,
            MatchCandidateCount = latestOpportunity == null || !context.CandidateCounts.TryGetValue(latestOpportunity.Id, out var count) ? 0 : count,
            MatchDisplayStatus = GetDisplayStatus(status, remainingQuantity, hasQuantity),
            MatchedQuantity = matchedQuantity,
            RemainingQuantity = remainingQuantity,
            OccupancyStatus = occupiedBy.Count > 0 ? "占用" : "空闲",
            IsOccupied = occupiedBy.Count > 0,
            OccupiedOpportunityIds = occupiedBy,
            MatchStatus = hasQuantity
                ? (remainingQuantity <= 0
                    ? "成交"
                    : occupiedBy.Count > 0 ? "撮合中" : "空闲")
                : (context.MatchedItemIds.Contains(item.Id)
                    ? "成交"
                    : occupiedBy.Count > 0 ? "撮合中" : "空闲")
        };
    }

    public static CrmVendorDemandMatchProgressDto BuildMatchProgress(IEnumerable<CrmVendorDemandItemDto> items)
    {
        var values = items.ToList();
        return new CrmVendorDemandMatchProgressDto
        {
            TotalItemCount = values.Count,
            // 业务状态桶（与 MatchStatus 对齐：成交 / 撮合中 / 空闲）
            CompletedItemCount = values.Count(item => item.MatchStatus == "成交"),
            InProgressItemCount = values.Count(item => item.MatchStatus == "撮合中"),
            PendingItemCount = values.Count(item => item.MatchStatus == "空闲"),
            // 机会状态桶（保留机会视角的补充信息）
            StartedItemCount = values.Count(item => item.MatchOpportunityId.HasValue),
            PausedItemCount = values.Count(item => item.MatchDisplayStatus == "已暂停"),
            ClosedItemCount = values.Count(item => item.MatchDisplayStatus == "已结束")
        };
    }

    private static string GetDisplayStatus(string status, decimal remainingQuantity, bool hasQuantity)
    {
        if (hasQuantity)
        {
            if (remainingQuantity <= 0) return "已中选完成";
            return status switch
            {
                CrmMatchOpportunity.InProgress => "撮合中",
                CrmMatchOpportunity.Paused => "已暂停",
                _ => "待撮合"
            };
        }
        return status switch
        {
            CrmMatchOpportunity.InProgress => "撮合中",
            CrmMatchOpportunity.Paused => "已暂停",
            CrmMatchOpportunity.Closed => "已结束",
            _ => "待撮合"
        };
    }

    public static async Task EnsureNoUnclosedMatchOpportunitiesAsync(
        ICrmDbContext dbContext,
        Guid demandId,
        CancellationToken cancellationToken)
    {
        // 主表已无 DemandId，通过需求单的明细集合桥接到子表 EntityId。
        var itemIds = await dbContext.CrmVendorDemandItems
            .Where(item => item.VendorDemandId == demandId)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        if (itemIds.Count == 0) return;
        var exists = await dbContext.CrmMatchOpportunityItems.AnyAsync(
            item => item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                itemIds.Contains(item.EntityId) &&
                dbContext.CrmMatchOpportunities.Any(opportunity =>
                    opportunity.Id == item.OpportunityId &&
                    (opportunity.Status == CrmMatchOpportunity.InProgress ||
                     opportunity.Status == CrmMatchOpportunity.Paused)),
            cancellationToken);
        if (exists)
        {
            throw new BusinessException(400, "存在未关闭的撮合机会，请先关闭撮合机会");
        }
    }

    public static async Task EnsureItemsNotReferencedByMatchOpportunityAsync(
        ICrmDbContext dbContext,
        IReadOnlyCollection<Guid> demandItemIds,
        CancellationToken cancellationToken)
    {
        if (demandItemIds.Count == 0) return;
        var exists = await dbContext.CrmMatchOpportunityItems.AnyAsync(
            item => item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                demandItemIds.Contains(item.EntityId),
            cancellationToken);
        if (exists)
        {
            throw new BusinessException(400, "采购明细已关联撮合机会，不能删除");
        }
    }

    public static async Task EnsureUnclosedMatchOpportunitiesNotAffectedByUpdateAsync(
        ICrmDbContext dbContext,
        CrmVendorDemand demand,
        CrmVendorDemandSaveRequest request,
        CancellationToken cancellationToken)
    {
        // 主表已无 DemandId，通过当前需求单明细集合桥接到子表，找出未关闭机会涉及的明细。
        var currentItemIds = demand.Items.Select(item => item.Id).ToList();
        if (currentItemIds.Count == 0) return;
        var protectedItemIds = await dbContext.CrmMatchOpportunityItems
            .Where(item => item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                currentItemIds.Contains(item.EntityId) &&
                dbContext.CrmMatchOpportunities.Any(opportunity =>
                    opportunity.Id == item.OpportunityId &&
                    (opportunity.Status == CrmMatchOpportunity.InProgress || opportunity.Status == CrmMatchOpportunity.Paused)))
            .Select(item => item.EntityId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (protectedItemIds.Count == 0)
        {
            return;
        }

        var requestItems = request.Items
            .Where(item => item.Id.HasValue)
            .ToDictionary(item => item.Id!.Value);
        foreach (var itemId in protectedItemIds)
        {
            var currentItem = demand.Items.FirstOrDefault(item => item.Id == itemId);
            if (currentItem == null || !requestItems.TryGetValue(itemId, out var requestItem))
            {
                throw new BusinessException(400, "存在未关闭的撮合机会，不能删除关联采购明细");
            }

            if (HasChanged(currentItem, requestItem))
            {
                throw new BusinessException(400, "存在未关闭的撮合机会，不能修改关联采购明细的撮合关键字段");
            }
        }
    }

    public static async Task RefreshLatestAsync(
        ICrmDbContext dbContext,
        CrmVendor vendor,
        CancellationToken cancellationToken,
        CrmVendorDemand? candidatePlan = null,
        Guid? excludedPlanId = null)
    {
        var latestPlan = await dbContext.CrmVendorDemands
            .Where(plan =>
                plan.VendorId == vendor.Id &&
                (!excludedPlanId.HasValue || plan.Id != excludedPlanId.Value))
            .OrderByDescending(plan => plan.DemandAt)
            .ThenByDescending(plan => plan.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidatePlan is not null &&
            candidatePlan.VendorId == vendor.Id &&
            (!excludedPlanId.HasValue || candidatePlan.Id != excludedPlanId.Value) &&
            IsLater(candidatePlan, latestPlan))
        {
            latestPlan = candidatePlan;
        }

        vendor.UpdateLatestPurchaseDemandSummary(
            latestPlan?.DemandAt,
            latestPlan?.DemandName ?? string.Empty);
    }

    private static bool IsLater(CrmVendorDemand plan, CrmVendorDemand? other)
    {
        if (other == null) return true;
        var planTime = plan.DemandAt;
        var otherTime = other.DemandAt;
        return planTime > otherTime || (planTime == otherTime && plan.CreatedAt > other.CreatedAt);
    }

    private static bool HasChanged(CrmVendorDemandItem item, CrmVendorDemandItemRequest request) =>
        !string.Equals(item.ProductName.Trim(), request.ProductName.Trim(), StringComparison.Ordinal) ||
        item.Quantity != request.Quantity ||
        !string.Equals(item.QuantityUnit.Trim(), request.QuantityUnit.Trim(), StringComparison.Ordinal) ||
        !string.Equals(item.Specification.Trim(), request.Specification.Trim(), StringComparison.Ordinal) ||
        !string.Equals(item.QualityRequirement.Trim(), request.QualityRequirement.Trim(), StringComparison.Ordinal) ||
        item.TargetPrice != request.TargetPrice ||
        !string.Equals(item.PriceUnit.Trim(), request.PriceUnit.Trim(), StringComparison.Ordinal) ||
        item.ExpectedDeliveryAt != request.ExpectedDeliveryAt;
}
