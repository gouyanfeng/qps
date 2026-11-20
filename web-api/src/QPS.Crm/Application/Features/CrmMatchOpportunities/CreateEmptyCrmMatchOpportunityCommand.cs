using QPS.Crm.Contracts;
using QPS.Crm.Contracts.Permissions;
using QPS.Crm.Domain.Entities;
using QPS.Crm.Application.Features.CrmVendors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmMatchOpportunities;

/// <summary>
/// 从列表页直接发起一条「只有发起方、没有候选方」的撮合机会。
/// 候选后续在详情页重新选择。
/// </summary>
[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class CreateEmptyCrmMatchOpportunityCommand : IRequest<Guid>
{
    public string Source { get; set; } = "demand";
    public Guid? DemandItemId { get; set; }
    public Guid? SupplyId { get; set; }
    public string NextActionRemark { get; set; } = string.Empty;
}

public class CreateEmptyCrmMatchOpportunityHandler(ICrmDbContext dbContext) : IRequestHandler<CreateEmptyCrmMatchOpportunityCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmptyCrmMatchOpportunityCommand request, CancellationToken cancellationToken)
    {
        var source = request.Source == "supply" ? "supply" : "demand";
        var nextActionAt = DateTime.Today.AddDays(3);
        var nextActionRemark = string.IsNullOrWhiteSpace(request.NextActionRemark)
            ? (source == "supply" ? "待选择候选采购需求" : "待选择候选基地")
            : request.NextActionRemark.Trim();
        CrmMatchOpportunity opportunity;
        string initiatorName;
        string activity;

        if (source == "supply")
        {
            var supplyId = request.SupplyId ?? Guid.Empty;
            var supply = await dbContext.CrmHerbBaseSupplies
                .Include(item => item.HerbBase)
                .FirstOrDefaultAsync(item => item.Id == supplyId, cancellationToken)
                ?? throw new BusinessException(404, "供应信息不存在");
            if (supply.HerbBase == null) throw new BusinessException(400, "供应信息不完整");
            if (supply.Status != CrmHerbBaseSupply.Active)
            {
                throw new BusinessException(400, "仅有效供应可以发起撮合");
            }
            if (supply.ValidUntil.HasValue && supply.ValidUntil.Value.Date < DateTime.Today)
            {
                throw new BusinessException(400, "供应信息已过有效期");
            }
            await CrmMatchOpportunitySource.EnsureSuppliesNotOccupiedElsewhereAsync(dbContext, [supply.Id], null, cancellationToken);

            opportunity = CrmMatchOpportunity.Create(null, "核实数量", nextActionAt, nextActionRemark, source);
            dbContext.CrmMatchOpportunities.Add(opportunity);
            dbContext.CrmMatchOpportunityItems.Add(
                CrmMatchOpportunitySource.CreateSupplyItem(opportunity, supply.Id, CrmMatchOpportunityItem.Initiator, 1));
            initiatorName = supply.HerbBase.SubjectName;
            activity = $"创建撮合机会（待选候选）：发起基地 {initiatorName} - {supply.ProductName}";
        }
        else
        {
            var demandItem = await CrmMatchOpportunitySource.LoadDemandItemAsync(
                dbContext, request.DemandItemId ?? Guid.Empty, cancellationToken);
            var demand = demandItem.VendorDemand!;
            var vendor = await dbContext.CrmVendors.FirstOrDefaultAsync(item => item.Id == demand.VendorId, cancellationToken)
                ?? throw new BusinessException(404, "需求厂商不存在");

            // 与采购列表展示口径一致：机会数量取剩余量（需求数量 - 已中选），已中选完成不再允许发起
            var matchContext = await CrmVendorDemands.LoadItemMatchContextAsync(dbContext, [demandItem.Id], cancellationToken);
            var matchedQuantity = matchContext.MatchedQuantities.GetValueOrDefault(demandItem.Id);
            var remainingQuantity = (demandItem.Quantity ?? 0m) - matchedQuantity;
            if (remainingQuantity <= 0)
            {
                throw new BusinessException(400, "该采购明细已中选完成，无需继续撮合");
            }
            await CrmMatchOpportunitySource.EnsureDemandItemsNotOccupiedElsewhereAsync(dbContext, [demandItem.Id], null, cancellationToken);
            opportunity = CrmMatchOpportunity.Create(vendor.OwnerUserId, "核实数量", nextActionAt, nextActionRemark, source);
            dbContext.CrmMatchOpportunities.Add(opportunity);
            dbContext.CrmMatchOpportunityItems.Add(
                CrmMatchOpportunitySource.CreateDemandItem(opportunity, demandItem.Id, CrmMatchOpportunityItem.Initiator, 1));
            initiatorName = vendor.VendorName;
            activity = $"创建撮合机会（待选候选）：发起药厂 {initiatorName} - {demandItem.ProductName}";
        }

        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(opportunity.Id, "创建机会", activity));
        await dbContext.SaveChangesAsync(cancellationToken);
        return opportunity.Id;
    }
}

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class ReplaceCrmMatchOpportunityCandidatesCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public List<Guid> SupplyIds { get; set; } = [];
    public List<Guid> DemandItemIds { get; set; } = [];
}

public class ReplaceCrmMatchOpportunityCandidatesHandler(ICrmDbContext dbContext) : IRequestHandler<ReplaceCrmMatchOpportunityCandidatesCommand, bool>
{
    public async Task<bool> Handle(ReplaceCrmMatchOpportunityCandidatesCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await CrmMatchOpportunityCommandGuards.FindOpportunityAsync(dbContext, request.OpportunityId, cancellationToken);
        CrmMatchOpportunityCommandGuards.EnsureInProgress(opportunity);

        // 供应侧机会的候选是药厂需求；需求侧机会的候选是基地供应。
        if (opportunity.Source == "supply")
        {
            CrmMatchOpportunitySource.ValidateDemandItemIds(request.DemandItemIds);
            await CrmMatchOpportunitySource.EnsureDemandItemsNotOccupiedElsewhereAsync(dbContext, request.DemandItemIds, opportunity.Id, cancellationToken);
            var items = await CrmMatchOpportunitySource.LoadEligibleDemandItemsAsync(dbContext, request.DemandItemIds, cancellationToken);
            var vendorIds = items.Select(item => item.VendorDemand!.VendorId).Distinct().ToList();
            var vendors = await dbContext.CrmVendors.Where(item => vendorIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);

            var existing = await dbContext.CrmMatchOpportunityItems
                .Where(item => item.OpportunityId == opportunity.Id && item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand)
                .ToListAsync(cancellationToken);
            var initiatorCount = existing.Count(item => item.Role == CrmMatchOpportunityItem.Initiator);
            dbContext.CrmMatchOpportunityItems.RemoveRange(existing.Where(item => item.Role != CrmMatchOpportunityItem.Initiator));

            foreach (var (item, index) in items.OrderBy(x => request.DemandItemIds.IndexOf(x.Id)).Select((item, index) => (item, index)))
            {
                dbContext.CrmMatchOpportunityItems.Add(
                    CrmMatchOpportunitySource.CreateDemandItem(opportunity, item.Id, CrmMatchOpportunityItem.Candidate, initiatorCount + index + 1));
            }

            dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(
                opportunity.Id,
                "重新选择候选",
                $"重新选择候选：{string.Join("、", items.Select(item => vendors[item.VendorDemand!.VendorId].VendorName + " - " + item.ProductName))}"));
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        CrmMatchOpportunitySource.ValidateSupplyIds(request.SupplyIds);
        await CrmMatchOpportunitySource.EnsureSuppliesNotOccupiedElsewhereAsync(dbContext, request.SupplyIds, opportunity.Id, cancellationToken);
        var supplies = await CrmMatchOpportunitySource.LoadEligibleSuppliesAsync(dbContext, request.SupplyIds, cancellationToken);

        var existingItems = await dbContext.CrmMatchOpportunityItems
            .Where(item => item.OpportunityId == opportunity.Id && item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply)
            .ToListAsync(cancellationToken);
        var baseInitiatorCount = existingItems.Count(item => item.Role == CrmMatchOpportunityItem.Initiator);
        dbContext.CrmMatchOpportunityItems.RemoveRange(existingItems.Where(item => item.Role != CrmMatchOpportunityItem.Initiator));

        foreach (var (supply, index) in supplies.OrderBy(item => request.SupplyIds.IndexOf(item.Id)).Select((item, index) => (item, index)))
        {
            dbContext.CrmMatchOpportunityItems.Add(
                CrmMatchOpportunitySource.CreateSupplyItem(opportunity, supply.Id, CrmMatchOpportunityItem.Candidate, baseInitiatorCount + index + 1));
        }

        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(
            opportunity.Id,
            "重新选择候选",
            $"重新选择候选：{string.Join("、", supplies.Select(item => item.HerbBase!.BaseName + " - " + item.ProductName))}"));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
