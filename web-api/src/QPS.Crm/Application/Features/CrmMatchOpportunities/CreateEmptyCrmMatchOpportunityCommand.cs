using QPS.Crm.Contracts;
using QPS.Crm.Contracts.Permissions;
using QPS.Crm.Domain.Entities;
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
}

public class CreateEmptyCrmMatchOpportunityHandler(ICrmDbContext dbContext) : IRequestHandler<CreateEmptyCrmMatchOpportunityCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmptyCrmMatchOpportunityCommand request, CancellationToken cancellationToken)
    {
        var source = request.Source == "supply" ? "supply" : "demand";
        var nextActionAt = DateTime.Today.AddDays(3);
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

            opportunity = CrmMatchOpportunity.Create(
                null,
                Guid.Empty,
                Guid.Empty,
                supply.ProductName,
                supply.AvailableQuantity,
                supply.QuantityUnit,
                supply.Specification,
                supply.QualityRequirement,
                null,
                supply.HerbBase.Address ?? string.Empty,
                null,
                "核实数量",
                nextActionAt,
                "待选择候选采购需求",
                source);
            dbContext.CrmMatchOpportunities.Add(opportunity);
            dbContext.CrmMatchCandidates.Add(CrmMatchOpportunitySource.CreateCandidate(
                opportunity, supply, 1, "initiator"));
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
            opportunity = CrmMatchOpportunity.Create(
                demandItem.Id,
                demand.Id,
                demand.VendorId,
                demandItem.ProductName,
                demandItem.Quantity,
                demandItem.QuantityUnit,
                demandItem.Specification,
                demandItem.QualityRequirement,
                demandItem.ExpectedDeliveryAt ?? demand.ExpectedDeliveryAt,
                demand.ReceivingAddress,
                vendor.OwnerUserId,
                "核实数量",
                nextActionAt,
                "待选择候选基地",
                source);
            dbContext.CrmMatchOpportunities.Add(opportunity);
            dbContext.CrmMatchOpportunityDemands.Add(CrmMatchOpportunityDemand.Create(
                opportunity.Id,
                demandItem.Id,
                demand.VendorId,
                vendor.VendorName,
                demandItem.ProductName,
                demandItem.Quantity,
                demandItem.QuantityUnit,
                demandItem.Specification,
                demandItem.QualityRequirement,
                demandItem.TargetPrice,
                demandItem.PriceUnit,
                demandItem.ExpectedDeliveryAt ?? demand.ExpectedDeliveryAt,
                demand.ReceivingAddress,
                CrmMatchOpportunityDemand.Initiator,
                1));
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
}

public class ReplaceCrmMatchOpportunityCandidatesHandler(ICrmDbContext dbContext) : IRequestHandler<ReplaceCrmMatchOpportunityCandidatesCommand, bool>
{
    public async Task<bool> Handle(ReplaceCrmMatchOpportunityCandidatesCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await CrmMatchOpportunityCommandGuards.FindOpportunityAsync(dbContext, request.OpportunityId, cancellationToken);
        CrmMatchOpportunityCommandGuards.EnsureInProgress(opportunity);
        CrmMatchOpportunitySource.ValidateSupplyIds(request.SupplyIds);
        var supplies = await CrmMatchOpportunitySource.LoadEligibleSuppliesAsync(dbContext, request.SupplyIds, cancellationToken);

        var existing = await dbContext.CrmMatchCandidates
            .Where(item => item.MatchOpportunityId == opportunity.Id)
            .ToListAsync(cancellationToken);
        var initiatorCount = existing.Count(item => item.Role == "initiator");
        dbContext.CrmMatchCandidates.RemoveRange(existing.Where(item => item.Role != "initiator"));

        var role = opportunity.Source == "supply" ? "candidate" : "candidate";
        foreach (var (supply, index) in supplies.OrderBy(item => request.SupplyIds.IndexOf(item.Id)).Select((item, index) => (item, index)))
        {
            dbContext.CrmMatchCandidates.Add(CrmMatchOpportunitySource.CreateCandidate(
                opportunity, supply, initiatorCount + index + 1, role));
        }

        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(
            opportunity.Id,
            "重新选择候选",
            $"重新选择候选：{string.Join("、", supplies.Select(item => item.HerbBase!.BaseName + " - " + item.ProductName))}"));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
