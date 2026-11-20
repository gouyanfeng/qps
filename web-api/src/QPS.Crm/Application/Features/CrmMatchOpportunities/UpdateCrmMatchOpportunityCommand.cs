using QPS.Crm.Contracts;
using QPS.Crm.Contracts.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmMatchOpportunities;

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class UpdateCrmMatchOpportunityItemStatusCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public Guid ItemId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string EliminationReason { get; set; } = string.Empty;
}

public class UpdateCrmMatchOpportunityItemStatusHandler(ICrmDbContext dbContext)
    : IRequestHandler<UpdateCrmMatchOpportunityItemStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateCrmMatchOpportunityItemStatusCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await CrmMatchOpportunityCommandGuards.FindOpportunityAsync(dbContext, request.OpportunityId, cancellationToken);
        CrmMatchOpportunityCommandGuards.EnsureInProgress(opportunity);
        var item = await dbContext.CrmMatchOpportunityItems.FirstOrDefaultAsync(
                x => x.Id == request.ItemId && x.OpportunityId == opportunity.Id, cancellationToken)
            ?? throw new BusinessException(404, "撮合明细不存在");

        // 前端按中文或英文提交均可，统一归一为英文枚举
        var status = CrmMatchOpportunityItem.NormalizeStatus(request.Status);
        if (status is not (CrmMatchOpportunityItem.Pending or CrmMatchOpportunityItem.Kept
            or CrmMatchOpportunityItem.Selected or CrmMatchOpportunityItem.Eliminated))
        {
            throw new BusinessException(400, "候选状态仅支持待评估、保留、中选或淘汰");
        }

        var label = await DescribeItemAsync(dbContext, item, cancellationToken);
        string actionType;
        if (status == CrmMatchOpportunityItem.Selected && item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply)
        {
            await CrmMatchOpportunitySource.LoadEligibleSuppliesAsync(dbContext, [item.EntityId], cancellationToken);
            await CrmMatchOpportunitySource.EnsureSupplyNotSelectedElsewhereAsync(
                dbContext, item.EntityId, opportunity.Id, cancellationToken);
        }
        if (status == CrmMatchOpportunityItem.Eliminated && string.IsNullOrWhiteSpace(request.EliminationReason))
        {
            throw new BusinessException(400, "淘汰撮合候选必须填写淘汰原因");
        }

        switch (status)
        {
            case CrmMatchOpportunityItem.Pending: item.ResetToPending(); actionType = "恢复候选"; break;
            case CrmMatchOpportunityItem.Kept: item.Keep(); actionType = "保留候选"; break;
            case CrmMatchOpportunityItem.Selected: item.Select(); actionType = "中选候选"; break;
            default: item.Eliminate(request.EliminationReason); actionType = "淘汰候选"; break;
        }

        var contentText = status switch
        {
            CrmMatchOpportunityItem.Pending => $"恢复候选为待评估：{label}",
            CrmMatchOpportunityItem.Kept => $"保留候选：{label}",
            CrmMatchOpportunityItem.Selected => $"中选候选：{label}",
            _ => $"淘汰候选：{label}，原因：{item.EliminationReason}"
        };
        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(opportunity.Id, actionType, contentText));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<string> DescribeItemAsync(ICrmDbContext dbContext, CrmMatchOpportunityItem item, CancellationToken cancellationToken)
    {
        if (item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply)
        {
            var supply = await dbContext.CrmHerbBaseSupplies
                .Include(x => x.HerbBase)
                .FirstOrDefaultAsync(x => x.Id == item.EntityId, cancellationToken);
            return supply == null ? "已失效供应" : $"{supply.HerbBase?.BaseName ?? string.Empty} - {supply.ProductName}";
        }
        else
        {
            var demandItem = await dbContext.CrmVendorDemandItems
                .Include(x => x.VendorDemand)
                .FirstOrDefaultAsync(x => x.Id == item.EntityId, cancellationToken);
            if (demandItem?.VendorDemand == null) return "已失效采购明细";
            var vendor = await dbContext.CrmVendors.FirstOrDefaultAsync(x => x.Id == demandItem.VendorDemand.VendorId, cancellationToken);
            return $"{vendor?.VendorName ?? string.Empty} - {demandItem.ProductName}";
        }
    }
}

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class AdvanceCrmMatchOpportunityCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string CurrentResult { get; set; } = string.Empty;
    public string NextAction { get; set; } = string.Empty;
    public string NextActionRemark { get; set; } = string.Empty;
    public DateTime? NextActionAt { get; set; }
}

public class AdvanceCrmMatchOpportunityHandler(ICrmDbContext dbContext) : IRequestHandler<AdvanceCrmMatchOpportunityCommand, bool>
{
    public async Task<bool> Handle(AdvanceCrmMatchOpportunityCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await CrmMatchOpportunityCommandGuards.FindOpportunityAsync(dbContext, request.OpportunityId, cancellationToken);
        if (opportunity.Status == CrmMatchOpportunity.Closed)
        {
            throw new BusinessException(400, "已关闭撮合机会不可编辑");
        }

        if (string.IsNullOrWhiteSpace(request.CurrentResult))
        {
            throw new BusinessException(400, "本次结果不能为空");
        }

        try
        {
            if (opportunity.Status == CrmMatchOpportunity.Paused)
            {
                if (request.NextActionAt <= DateTime.Now)
                {
                    throw new BusinessException(400, "恢复暂停机会时计划时间必须晚于当前时间");
                }
                opportunity.Resume();
            }
            opportunity.Advance(request.Stage, request.NextAction, request.NextActionRemark, request.NextActionAt);
        }
        catch (InvalidOperationException exception)
        {
            throw new BusinessException(400, exception.Message);
        }

        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(
            opportunity.Id,
            "推进",
            $"推进阶段：{opportunity.Stage}；本次结果：{request.CurrentResult.Trim()}；下一步：{CrmMatchOpportunitySource.DescribeNextAction(opportunity.NextAction, opportunity.NextActionRemark)}；计划时间：{opportunity.NextActionAt:yyyy-MM-dd HH:mm}"));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class UpdateCrmMatchOpportunityStatusCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CloseResult { get; set; } = string.Empty;
    public string ClosedReason { get; set; } = string.Empty;
}

public class UpdateCrmMatchOpportunityStatusHandler(ICrmDbContext dbContext) : IRequestHandler<UpdateCrmMatchOpportunityStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateCrmMatchOpportunityStatusCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await CrmMatchOpportunityCommandGuards.FindOpportunityAsync(dbContext, request.OpportunityId, cancellationToken);
        var status = request.Status.Trim();
        string actionType;
        string content;
        if (status == CrmMatchOpportunity.Paused)
        {
            if (opportunity.Status != CrmMatchOpportunity.InProgress)
            {
                throw new BusinessException(400, "仅推进中的撮合机会可以暂停");
            }
            opportunity.Pause();
            actionType = "暂停";
            content = "暂停撮合机会";
        }
        else if (status == CrmMatchOpportunity.Closed)
        {
            if (opportunity.Status == CrmMatchOpportunity.Closed)
            {
                throw new BusinessException(400, "已关闭撮合机会不可编辑");
            }

            var closeResult = request.CloseResult.Trim();
            if (closeResult is not (CrmMatchOpportunity.Matched or CrmMatchOpportunity.NotMatched or CrmMatchOpportunity.DemandCancelled))
            {
                throw new BusinessException(400, "撮合机会关闭结果无效");
            }

            if (closeResult == CrmMatchOpportunity.Matched && opportunity.Stage != CrmMatchOpportunityStage.IntentConfirmation)
            {
                throw new BusinessException(400, "仅意向确认阶段可以撮合成功关闭");
            }

            var selectedSupplyIds = await dbContext.CrmMatchOpportunityItems
                .Where(item => item.OpportunityId == opportunity.Id &&
                    item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                    item.Status == CrmMatchOpportunityItem.Selected)
                .Select(item => item.EntityId)
                .ToListAsync(cancellationToken);
            var selectedDemandCount = await dbContext.CrmMatchOpportunityItems.CountAsync(
                    item => item.OpportunityId == opportunity.Id &&
                        item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                        item.Role == CrmMatchOpportunityItem.Candidate &&
                        item.Status == CrmMatchOpportunityItem.Selected,
                    cancellationToken);
            if (closeResult == CrmMatchOpportunity.Matched)
            {
                if (opportunity.Source == "supply")
                {
                    if (selectedDemandCount == 0)
                    {
                        throw new BusinessException(400, "撮合成功必须至少中选一条候选药厂需求");
                    }
                }
                else
                {
                    if (selectedSupplyIds.Count == 0)
                    {
                        throw new BusinessException(400, "撮合成功必须至少中选一条候选供应信息");
                    }

                    await CrmMatchOpportunitySource.LoadEligibleSuppliesAsync(dbContext, selectedSupplyIds, cancellationToken);
                    foreach (var supplyId in selectedSupplyIds)
                    {
                        await CrmMatchOpportunitySource.EnsureSupplyNotSelectedElsewhereAsync(
                            dbContext, supplyId, opportunity.Id, cancellationToken);
                    }
                }
            }
            else if (selectedSupplyIds.Count > 0 || selectedDemandCount > 0)
            {
                throw new BusinessException(400, "未成交或需求取消前请先将中选候选改为保留");
            }

            try
            {
                opportunity.Close(closeResult, request.ClosedReason);
            }
            catch (InvalidOperationException exception)
            {
                throw new BusinessException(400, exception.Message);
            }

            if (closeResult == CrmMatchOpportunity.Matched)
            {
                // 成交后需在供应侧留下明确标记
                var soldSupplyIds = new List<Guid>(selectedSupplyIds);
                if (opportunity.Source == "supply")
                {
                    soldSupplyIds.AddRange(await dbContext.CrmMatchOpportunityItems
                        .Where(item => item.OpportunityId == opportunity.Id &&
                            item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                            item.Role == CrmMatchOpportunityItem.Initiator)
                        .Select(item => item.EntityId)
                        .ToListAsync(cancellationToken));
                }

                var soldSupplies = await dbContext.CrmHerbBaseSupplies
                    .Where(item => soldSupplyIds.Contains(item.Id))
                    .ToListAsync(cancellationToken);
                foreach (var supply in soldSupplies)
                {
                    if (supply.Status is CrmHerbBaseSupply.SoldOut or CrmHerbBaseSupply.Expired) continue;
                    supply.ChangeStatus(CrmHerbBaseSupply.SoldOut);
                }

                // 双方数量不一致时，未被成交覆盖的差额自动生成新的采购明细或供应信息
                var balanceNote = await CrmMatchOpportunityBalance.CreateAsync(dbContext, opportunity, cancellationToken);
                if (!string.IsNullOrEmpty(balanceNote))
                {
                    dbContext.CrmBusinessActivities.Add(
                        CrmMatchOpportunitySource.CreateActivity(opportunity.Id, "生成差额明细", balanceNote));
                }
            }
            actionType = "关闭";
            content = string.IsNullOrWhiteSpace(opportunity.ClosedReason)
                ? $"关闭撮合机会，结果：{opportunity.CloseResult}"
                : $"关闭撮合机会，结果：{opportunity.CloseResult}，原因：{opportunity.ClosedReason}";
        }
        else
        {
            throw new BusinessException(400, "撮合机会状态无效");
        }

        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(opportunity.Id, actionType, content));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class CrmMatchOpportunityCommandGuards
{
    public static async Task<CrmMatchOpportunity> FindOpportunityAsync(ICrmDbContext dbContext, Guid opportunityId, CancellationToken cancellationToken) =>
        await dbContext.CrmMatchOpportunities.FirstOrDefaultAsync(item => item.Id == opportunityId, cancellationToken)
            ?? throw new BusinessException(404, "撮合机会不存在");

    public static void EnsureInProgress(CrmMatchOpportunity opportunity)
    {
        if (opportunity.Status != CrmMatchOpportunity.InProgress)
        {
            throw new BusinessException(400, opportunity.Status == CrmMatchOpportunity.Closed ? "已关闭撮合机会不可编辑" : "仅推进中的撮合机会可以维护候选");
        }
    }
}
