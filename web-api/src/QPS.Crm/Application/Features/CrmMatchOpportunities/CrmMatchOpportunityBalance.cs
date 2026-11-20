using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts;
using QPS.Crm.Domain.Entities;

namespace QPS.Crm.Application.Features.CrmMatchOpportunities;

/// <summary>
/// 撮合成功关闭时的数量拆分：成交数量取采购侧与供应侧的较小值。
/// 数量较大的一方按「成交量 + 空闲余量」拆分：原明细扣掉空闲余量（保留本次成交部分），
/// 空闲余量复制为新的采购明细或供应信息。不再存快照，全部基于原明细实时数量。
/// </summary>
internal static class CrmMatchOpportunityBalance
{
    /// <summary>生成差额明细，返回可写入业务动态的说明；无差额时返回空字符串。</summary>
    public static async Task<string> CreateAsync(
        ICrmDbContext dbContext,
        CrmMatchOpportunity opportunity,
        CancellationToken cancellationToken)
    {
        var isDemandSource = opportunity.Source != "supply";

        // 供应侧：需求发起取中选候选供应，供应发起取发起供应。
        var supplyEntityIds = await (
                from item in dbContext.CrmMatchOpportunityItems
                where item.OpportunityId == opportunity.Id &&
                    item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                    (isDemandSource
                        ? item.Status == CrmMatchOpportunityItem.Selected
                        : item.Role == CrmMatchOpportunityItem.Initiator)
                orderby item.SortOrder
                select item.EntityId)
            .ToListAsync(cancellationToken);
        var supplies = await dbContext.CrmHerbBaseSupplies
            .Where(item => supplyEntityIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        var supplyById = supplies.ToDictionary(item => item.Id);

        // 需求侧：需求发起取发起方明细，供应发起取中选的候选采购明细。
        var demandEntityIds = await (
                from item in dbContext.CrmMatchOpportunityItems
                where item.OpportunityId == opportunity.Id &&
                    item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                    (isDemandSource
                        ? item.Role == CrmMatchOpportunityItem.Initiator
                        : item.Status == CrmMatchOpportunityItem.Selected)
                orderby item.SortOrder
                select item.EntityId)
            .ToListAsync(cancellationToken);
        var demandItems = await dbContext.CrmVendorDemandItems
            .Where(item => demandEntityIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        var demandById = demandItems.ToDictionary(item => item.Id);

        var supplyTotal = SumQuantity(supplies.Select(item => item.AvailableQuantity));
        var demandTotal = SumQuantity(demandItems.Select(item => item.Quantity));
        if (supplyTotal is null or <= 0 || demandTotal is null or <= 0)
        {
            return string.Empty;
        }

        var dealQuantity = Math.Min(demandTotal.Value, supplyTotal.Value);
        var notes = new List<string>();

        // 采购侧拆分：按 SortOrder 逐条分配成交量，余量从原明细扣除并另起一条空闲明细。
        var demandRemaining = dealQuantity;
        foreach (var entityId in demandEntityIds)
        {
            if (!demandById.TryGetValue(entityId, out var demandItem)) continue;
            var quantity = demandItem.Quantity.GetValueOrDefault();
            var allocated = Math.Min(quantity, demandRemaining);
            demandRemaining -= allocated;
            var leftover = quantity - allocated;
            if (leftover <= 0) continue;

            demandItem.SetQuantity(Math.Max(0m, quantity - leftover));
            await AddDemandItemAsync(dbContext, demandItem, leftover, cancellationToken);
            notes.Add($"采购明细 {demandItem.ProductName} {leftover}{demandItem.QuantityUnit}");
        }

        // 供应侧拆分：按 SortOrder 逐条分配成交量，余量从原明细扣除并另起一条空闲供应。
        var supplyRemaining = dealQuantity;
        foreach (var entityId in supplyEntityIds)
        {
            if (!supplyById.TryGetValue(entityId, out var supply)) continue;
            var quantity = supply.AvailableQuantity.GetValueOrDefault();
            var allocated = Math.Min(quantity, supplyRemaining);
            supplyRemaining -= allocated;
            var leftover = quantity - allocated;
            if (leftover <= 0) continue;

            supply.SetAvailableQuantity(Math.Max(0m, quantity - leftover));
            var generated = CrmHerbBaseSupply.Create(
                supply.HerbBaseId,
                supply.HerbBaseSubjectId,
                supply.ProductName,
                leftover,
                supply.QuantityUnit,
                supply.Specification,
                supply.QualityRequirement,
                supply.HarvestSeason,
                supply.ExpectedPrice,
                supply.PriceUnit,
                supply.SupplyCycle,
                supply.ConfirmedAt,
                supply.ValidUntil,
                supply.Remark);
            generated.ChangeStatus(CrmHerbBaseSupply.Active);
            dbContext.CrmHerbBaseSupplies.Add(generated);
            notes.Add($"供应信息 {supply.ProductName} {leftover}{supply.QuantityUnit}");
        }

        return notes.Count == 0 ? string.Empty : $"数量差额自动生成：{string.Join("、", notes)}";
    }

    private static async Task AddDemandItemAsync(
        ICrmDbContext dbContext,
        CrmVendorDemandItem source,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        var demand = await dbContext.CrmVendorDemands
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == source.VendorDemandId, cancellationToken);
        if (demand == null)
        {
            return;
        }

        var sortOrder = demand.Items.Count == 0 ? 1 : demand.Items.Max(item => item.SortOrder) + 1;
        var generated = new CrmVendorDemandItem(
            source.ProductName,
            quantity,
            source.QuantityUnit,
            source.Specification,
            source.QualityRequirement,
            source.TargetPrice,
            source.PriceUnit,
            source.ExpectedDeliveryAt,
            source.Remark,
            sortOrder);
        demand.Items.Add(generated);
        // 显式标记为新增，避免带主键的实体经导航集合被发现时被当作已有实体更新
        dbContext.CrmVendorDemandItems.Add(generated);
    }

    private static decimal? SumQuantity(IEnumerable<decimal?> quantities)
    {
        var values = quantities.ToList();
        if (values.Count == 0 || values.All(item => item == null))
        {
            return null;
        }

        return values.Sum(item => item.GetValueOrDefault());
    }
}
