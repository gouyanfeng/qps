using QPS.Crm.Contracts;
using QPS.Crm.Contracts.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Exceptions;
using QPS.Crm.Application.Features.CrmVendors;

namespace QPS.Crm.Application.Features.CrmMatchOpportunities;

/// <summary>供应侧发起撮合：必须先选定候选采购明细，才创建撮合机会。</summary>
[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class CreateSupplyMatchOpportunityCommand : IRequest<Guid>
{
    public Guid SupplyId { get; set; }
    public List<Guid> DemandItemIds { get; set; } = [];
    public string NextAction { get; set; } = string.Empty;
    public string NextActionRemark { get; set; } = string.Empty;
    public DateTime? NextActionAt { get; set; }
}

public class CreateSupplyMatchOpportunityHandler(ICrmDbContext dbContext)
    : IRequestHandler<CreateSupplyMatchOpportunityCommand, Guid>
{
    public async Task<Guid> Handle(CreateSupplyMatchOpportunityCommand request, CancellationToken cancellationToken)
    {
        var supply = await dbContext.CrmHerbBaseSupplies
            .Include(item => item.HerbBase)
            .FirstOrDefaultAsync(item => item.Id == request.SupplyId, cancellationToken)
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

        var demandItemIds = request.DemandItemIds.Distinct().ToList();
        if (demandItemIds.Count == 0)
        {
            throw new BusinessException(400, "发起撮合必须至少选择一条候选采购明细");
        }

        var demandItems = await dbContext.CrmVendorDemandItems
            .Include(item => item.VendorDemand)
            .Where(item => demandItemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        if (demandItems.Count != demandItemIds.Count)
        {
            throw new BusinessException(400, "候选采购明细不存在");
        }

        await CrmMatchOpportunitySource.EnsureSuppliesNotOccupiedElsewhereAsync(
            dbContext, [supply.Id], null, cancellationToken);
        await CrmMatchOpportunitySource.EnsureDemandItemsNotOccupiedElsewhereAsync(
            dbContext, demandItemIds, null, cancellationToken);

        // 已中选数量口径与采购列表一致：按中选记录的实际数量统计，而非机会整单需求量。
        var matchContext = await CrmVendorDemands.LoadItemMatchContextAsync(dbContext, demandItemIds, cancellationToken);
        foreach (var item in demandItems)
        {
            var remaining = (item.Quantity ?? 0m) - matchContext.MatchedQuantities.GetValueOrDefault(item.Id);
            if (item.Quantity.HasValue && item.Quantity.Value > 0 && remaining <= 0)
            {
                throw new BusinessException(400, $"采购明细「{item.ProductName}」已采购完成，无需继续撮合");
            }
        }

        var nextAction = string.IsNullOrWhiteSpace(request.NextAction) ? "核实数量" : request.NextAction.Trim();
        var nextActionAt = request.NextActionAt ?? DateTime.Today.AddDays(3);
        var opportunity = CrmMatchOpportunity.Create(
            supply.HerbBase.OwnerUserId,
            nextAction,
            nextActionAt,
            request.NextActionRemark,
            "supply");
        dbContext.CrmMatchOpportunities.Add(opportunity);
        // 供应发起方 1 条
        dbContext.CrmMatchOpportunityItems.Add(
            CrmMatchOpportunitySource.CreateSupplyItem(opportunity, supply.Id, CrmMatchOpportunityItem.Initiator, 1));

        var orderedDemandItems = demandItemIds
            .Select(id => demandItems.First(item => item.Id == id))
            .ToList();
        foreach (var (item, index) in orderedDemandItems.Select((item, index) => (item, index)))
        {
            dbContext.CrmMatchOpportunityItems.Add(
                CrmMatchOpportunitySource.CreateDemandItem(opportunity, item.Id, CrmMatchOpportunityItem.Candidate, index + 2));
        }

        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(
            opportunity.Id,
            "创建机会",
            $"创建撮合机会（已选候选）：发起基地 {supply.HerbBase.SubjectName} - {supply.ProductName}；候选采购明细 {demandItems.Count} 条"));
        foreach (var item in orderedDemandItems)
        {
            var demand = item.VendorDemand!;
            var vendor = await dbContext.CrmVendors.FirstOrDefaultAsync(v => v.Id == demand.VendorId, cancellationToken);
            dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(
                opportunity.Id,
                "加入候选",
                $"加入候选：{vendor?.VendorName ?? "-"} - {item.ProductName}"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return opportunity.Id;
    }
}
