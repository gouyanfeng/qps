using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Attributes;
using QPS.Application.Features.System;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmMatchOpportunities;

[Permission(SystemPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class CreateCrmMatchOpportunityCommand : IRequest<Guid>
{
    public Guid DemandItemId { get; set; }
    public List<Guid> SupplyIds { get; set; } = [];
    public string NextAction { get; set; } = string.Empty;
    public DateTime? NextActionAt { get; set; }
}

public class CreateCrmMatchOpportunityHandler(IDbContext dbContext) : IRequestHandler<CreateCrmMatchOpportunityCommand, Guid>
{
    public async Task<Guid> Handle(CreateCrmMatchOpportunityCommand request, CancellationToken cancellationToken)
    {
        CrmMatchOpportunitySource.ValidateSupplyIds(request.SupplyIds);
        CrmMatchOpportunitySource.ValidateNextAction(request.NextAction, request.NextActionAt);
        var demandItem = await CrmMatchOpportunitySource.LoadDemandItemAsync(dbContext, request.DemandItemId, cancellationToken);
        var demand = demandItem.VendorDemand!;
        if (await dbContext.CrmMatchOpportunities.AnyAsync(
                item => item.DemandItemId == demandItem.Id &&
                    (item.Status == CrmMatchOpportunity.InProgress || item.Status == CrmMatchOpportunity.Paused),
                cancellationToken))
        {
            throw new BusinessException(400, "该采购明细已有未关闭的撮合机会");
        }

        var vendor = await dbContext.CrmVendors.FirstOrDefaultAsync(item => item.Id == demand.VendorId && !item.IsDeleted, cancellationToken)
            ?? throw new BusinessException(404, "需求厂商不存在");
        var supplies = await CrmMatchOpportunitySource.LoadEligibleSuppliesAsync(dbContext, request.SupplyIds, cancellationToken);

        var opportunity = CrmMatchOpportunity.Create(
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
            request.NextAction,
            request.NextActionAt);
        dbContext.CrmMatchOpportunities.Add(opportunity);

        foreach (var (supply, index) in supplies.OrderBy(item => request.SupplyIds.IndexOf(item.Id)).Select((item, index) => (item, index)))
        {
            dbContext.CrmMatchCandidates.Add(CrmMatchOpportunitySource.CreateCandidate(opportunity, supply, index + 1));
        }

        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(opportunity.Id, "创建机会", $"创建撮合机会：采购品类{demandItem.ProductName}"));
        foreach (var supply in supplies.OrderBy(item => request.SupplyIds.IndexOf(item.Id)))
        {
            dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(opportunity.Id, "加入候选", $"加入候选：{supply.HerbBase!.BaseName} - {supply.ProductName}"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return opportunity.Id;
    }
}

[Permission(SystemPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class AddCrmMatchOpportunityCandidatesCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public List<Guid> SupplyIds { get; set; } = [];
}

public class AddCrmMatchOpportunityCandidatesHandler(IDbContext dbContext) : IRequestHandler<AddCrmMatchOpportunityCandidatesCommand, bool>
{
    public async Task<bool> Handle(AddCrmMatchOpportunityCandidatesCommand request, CancellationToken cancellationToken)
    {
        CrmMatchOpportunitySource.ValidateSupplyIds(request.SupplyIds);
        var opportunity = await dbContext.CrmMatchOpportunities.FirstOrDefaultAsync(item => item.Id == request.OpportunityId, cancellationToken)
            ?? throw new BusinessException(404, "撮合机会不存在");
        if (opportunity.Status != CrmMatchOpportunity.InProgress)
        {
            throw new BusinessException(400, opportunity.Status == CrmMatchOpportunity.Closed ? "已关闭撮合机会不可编辑" : "仅推进中的撮合机会可以追加候选");
        }

        if (await dbContext.CrmMatchCandidates.AnyAsync(item => item.MatchOpportunityId == opportunity.Id && request.SupplyIds.Contains(item.HerbBaseSupplyId), cancellationToken))
        {
            throw new BusinessException(400, "候选供应信息已存在");
        }

        var supplies = await CrmMatchOpportunitySource.LoadEligibleSuppliesAsync(dbContext, request.SupplyIds, cancellationToken);
        var startSortOrder = await dbContext.CrmMatchCandidates
            .Where(item => item.MatchOpportunityId == opportunity.Id)
            .Select(item => (int?)item.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        foreach (var (supply, index) in supplies.OrderBy(item => request.SupplyIds.IndexOf(item.Id)).Select((item, index) => (item, index)))
        {
            dbContext.CrmMatchCandidates.Add(CrmMatchOpportunitySource.CreateCandidate(opportunity, supply, startSortOrder + index + 1));
            dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(opportunity.Id, "加入候选", $"加入候选：{supply.HerbBase!.BaseName} - {supply.ProductName}"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
