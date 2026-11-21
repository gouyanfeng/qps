using QPS.Crm.Contracts;
using QPS.Crm.Contracts.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Exceptions;
using QPS.Crm.Application.Features.CrmVendors;

namespace QPS.Crm.Application.Features.CrmMatchOpportunities;

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class CreateCrmMatchOpportunityCommand : IRequest<Guid>
{
    public Guid DemandItemId { get; set; }
    /// <summary>兼容旧前端：只允许包含发起明细本身，不允许额外发起方（强制 1 发起方）。</summary>
    public List<Guid> DemandItemIds { get; set; } = [];
    public List<Guid> SupplyIds { get; set; } = [];
    public string NextAction { get; set; } = string.Empty;
    public string NextActionRemark { get; set; } = string.Empty;
    public DateTime? NextActionAt { get; set; }
    /// <summary>发起方向：demand=采购列表，supply=供应列表。</summary>
    public string Source { get; set; } = "demand";
}

public class CreateCrmMatchOpportunityHandler(ICrmDbContext dbContext) : IRequestHandler<CreateCrmMatchOpportunityCommand, Guid>
{
    public async Task<Guid> Handle(CreateCrmMatchOpportunityCommand request, CancellationToken cancellationToken)
    {
        CrmMatchOpportunitySource.ValidateSupplyIds(request.SupplyIds);
        await CrmMatchOpportunitySource.EnsureSuppliesNotOccupiedElsewhereAsync(dbContext, request.SupplyIds, null, cancellationToken);
        CrmMatchOpportunitySource.ValidateNextAction(request.NextAction, request.NextActionAt);
        try
        {
            CrmMatchOpportunityNextAction.Validate(
                CrmMatchOpportunityStage.MatchingVerification,
                request.NextAction.Trim(),
                request.NextActionRemark);
        }
        catch (InvalidOperationException exception)
        {
            throw new BusinessException(400, exception.Message);
        }

        // 强制 1 发起方：需求侧发起明细只能是主明细本身，不接受额外发起明细。
        var extraInitiatorIds = request.DemandItemIds.Where(id => id != request.DemandItemId).Distinct().ToList();
        if (extraInitiatorIds.Count > 0)
        {
            throw new BusinessException(400, "一个撮合机会只允许一条发起方明细");
        }

        var demandItem = await CrmMatchOpportunitySource.LoadDemandItemAsync(dbContext, request.DemandItemId, cancellationToken);
        var demand = demandItem.VendorDemand!;

        // 只有需求数量未撮合满时才可以继续撮合剩余量；已中选完成不再允许发起。
        var matchContext = await CrmVendorDemands.LoadItemMatchContextAsync(dbContext, [demandItem.Id], cancellationToken);
        var matchedQuantity = matchContext.MatchedQuantities.GetValueOrDefault(demandItem.Id);
        var remainingQuantity = (demandItem.Quantity ?? 0m) - matchedQuantity;
        if (remainingQuantity <= 0)
        {
            throw new BusinessException(400, "该采购明细已采购完成，无需继续撮合");
        }

        await CrmMatchOpportunitySource.EnsureDemandItemsNotOccupiedElsewhereAsync(dbContext, [demandItem.Id], null, cancellationToken);

        var vendor = await dbContext.CrmVendors.FirstOrDefaultAsync(item => item.Id == demand.VendorId, cancellationToken)
            ?? throw new BusinessException(404, "需求厂商不存在");
        var supplies = await CrmMatchOpportunitySource.LoadEligibleSuppliesAsync(dbContext, request.SupplyIds, cancellationToken);

        var opportunity = CrmMatchOpportunity.Create(
            vendor.OwnerUserId,
            request.NextAction,
            request.NextActionAt,
            request.NextActionRemark,
            request.Source);
        dbContext.CrmMatchOpportunities.Add(opportunity);

        // 1 发起方（采购明细）+ N 候选（供应明细）
        dbContext.CrmMatchOpportunityItems.Add(
            CrmMatchOpportunitySource.CreateDemandItem(opportunity, demandItem.Id, CrmMatchOpportunityItem.Initiator, 1));
        var demandSource = request.Source != "supply";
        var candidateRole = demandSource ? CrmMatchOpportunityItem.Candidate : CrmMatchOpportunityItem.Candidate;
        foreach (var (supply, index) in supplies.OrderBy(item => request.SupplyIds.IndexOf(item.Id)).Select((item, index) => (item, index)))
        {
            dbContext.CrmMatchOpportunityItems.Add(
                CrmMatchOpportunitySource.CreateSupplyItem(opportunity, supply.Id, candidateRole, index + 2));
        }

        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(
            opportunity.Id,
            "创建机会",
            $"创建撮合机会：采购品类{demandItem.ProductName}；发起方向：{(request.Source == "supply" ? "供应 → 需求" : "需求 → 供应")}；初始阶段：{opportunity.Stage}；下一步：{CrmMatchOpportunitySource.DescribeNextAction(opportunity.NextAction, opportunity.NextActionRemark)}；计划时间：{opportunity.NextActionAt:yyyy-MM-dd HH:mm}"));
        foreach (var supply in supplies.OrderBy(item => request.SupplyIds.IndexOf(item.Id)))
        {
            dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(opportunity.Id, "加入候选", $"加入候选：{supply.HerbBase!.BaseName} - {supply.ProductName}"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return opportunity.Id;
    }
}

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class AddCrmMatchOpportunityCandidatesCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public List<Guid> SupplyIds { get; set; } = [];
    public List<Guid> DemandItemIds { get; set; } = [];
}

public class AddCrmMatchOpportunityCandidatesHandler(ICrmDbContext dbContext) : IRequestHandler<AddCrmMatchOpportunityCandidatesCommand, bool>
{
    public async Task<bool> Handle(AddCrmMatchOpportunityCandidatesCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await dbContext.CrmMatchOpportunities.FirstOrDefaultAsync(item => item.Id == request.OpportunityId, cancellationToken)
            ?? throw new BusinessException(404, "撮合机会不存在");
        if (opportunity.Status != CrmMatchOpportunity.InProgress)
        {
            throw new BusinessException(400, opportunity.Status == CrmMatchOpportunity.Closed ? "已关闭撮合机会不可编辑" : "仅推进中的撮合机会可以追加候选");
        }

        // 供应侧机会追加药厂候选
        if (opportunity.Source == "supply")
        {
            CrmMatchOpportunitySource.ValidateDemandItemIds(request.DemandItemIds);
            await CrmMatchOpportunitySource.EnsureDemandItemsNotOccupiedElsewhereAsync(dbContext, request.DemandItemIds, opportunity.Id, cancellationToken);
            if (await dbContext.CrmMatchOpportunityItems.AnyAsync(
                    item => item.OpportunityId == opportunity.Id &&
                        item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                        request.DemandItemIds.Contains(item.EntityId), cancellationToken))
            {
                throw new BusinessException(400, "候选采购需求已存在");
            }

            var items = await CrmMatchOpportunitySource.LoadEligibleDemandItemsAsync(dbContext, request.DemandItemIds, cancellationToken);
            var vendorIds = items.Select(item => item.VendorDemand!.VendorId).Distinct().ToList();
            var vendors = await dbContext.CrmVendors.Where(item => vendorIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
            var startOrder = await dbContext.CrmMatchOpportunityItems
                .Where(item => item.OpportunityId == opportunity.Id)
                .Select(item => (int?)item.SortOrder).MaxAsync(cancellationToken) ?? 0;

            foreach (var (item, index) in items.OrderBy(x => request.DemandItemIds.IndexOf(x.Id)).Select((item, index) => (item, index)))
            {
                dbContext.CrmMatchOpportunityItems.Add(
                    CrmMatchOpportunitySource.CreateDemandItem(opportunity, item.Id, CrmMatchOpportunityItem.Candidate, startOrder + index + 1));
                dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(
                    opportunity.Id, "加入候选", $"加入候选：{vendors[item.VendorDemand!.VendorId].VendorName} - {item.ProductName}"));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        // 需求侧机会追加基地供应候选
        CrmMatchOpportunitySource.ValidateSupplyIds(request.SupplyIds);
        await CrmMatchOpportunitySource.EnsureSuppliesNotOccupiedElsewhereAsync(dbContext, request.SupplyIds, opportunity.Id, cancellationToken);
        if (await dbContext.CrmMatchOpportunityItems.AnyAsync(
                item => item.OpportunityId == opportunity.Id &&
                    item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                    request.SupplyIds.Contains(item.EntityId), cancellationToken))
        {
            throw new BusinessException(400, "候选供应信息已存在");
        }

        var supplies = await CrmMatchOpportunitySource.LoadEligibleSuppliesAsync(dbContext, request.SupplyIds, cancellationToken);
        var startSortOrder = await dbContext.CrmMatchOpportunityItems
            .Where(item => item.OpportunityId == opportunity.Id)
            .Select(item => (int?)item.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        foreach (var (supply, index) in supplies.OrderBy(item => request.SupplyIds.IndexOf(item.Id)).Select((item, index) => (item, index)))
        {
            dbContext.CrmMatchOpportunityItems.Add(
                CrmMatchOpportunitySource.CreateSupplyItem(opportunity, supply.Id, CrmMatchOpportunityItem.Candidate, startSortOrder + index + 1));
            dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(opportunity.Id, "加入候选", $"加入候选：{supply.HerbBase!.BaseName} - {supply.ProductName}"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
