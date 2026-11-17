using QPS.Crm.Contracts;
using QPS.Crm.Contracts.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmMatchOpportunities;

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class UpdateCrmMatchCandidateStatusCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public Guid CandidateId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string EliminationReason { get; set; } = string.Empty;
}

public class UpdateCrmMatchCandidateStatusHandler(ICrmDbContext dbContext) : IRequestHandler<UpdateCrmMatchCandidateStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateCrmMatchCandidateStatusCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await CrmMatchOpportunityCommandGuards.FindOpportunityAsync(dbContext, request.OpportunityId, cancellationToken);
        CrmMatchOpportunityCommandGuards.EnsureInProgress(opportunity);
        var candidate = await dbContext.CrmMatchCandidates.FirstOrDefaultAsync(item => item.Id == request.CandidateId && item.MatchOpportunityId == opportunity.Id, cancellationToken)
            ?? throw new BusinessException(404, "撮合候选不存在");

        var status = request.Status.Trim();
        string actionType;
        string content;
        if (status == CrmMatchCandidate.PendingEvaluation)
        {
            candidate.ResetToPending();
            actionType = "恢复候选";
            content = $"恢复候选为待评估：{candidate.HerbBaseName} - {candidate.ProductName}";
        }
        else if (status == CrmMatchCandidate.Kept)
        {
            candidate.Keep();
            actionType = "保留候选";
            content = $"保留候选：{candidate.HerbBaseName} - {candidate.ProductName}";
        }
        else if (status == CrmMatchCandidate.Selected)
        {
            await CrmMatchOpportunitySource.LoadEligibleSuppliesAsync(dbContext, [candidate.HerbBaseSupplyId], cancellationToken);
            await CrmMatchOpportunitySource.EnsureSupplyNotSelectedElsewhereAsync(
                dbContext,
                candidate.HerbBaseSupplyId,
                opportunity.Id,
                cancellationToken);
            candidate.Select();
            actionType = "中选候选";
            content = $"中选候选：{candidate.HerbBaseName} - {candidate.ProductName}";
        }
        else if (status == CrmMatchCandidate.Eliminated)
        {
            if (string.IsNullOrWhiteSpace(request.EliminationReason))
            {
                throw new BusinessException(400, "淘汰撮合候选必须填写淘汰原因");
            }

            candidate.Eliminate(request.EliminationReason);
            actionType = "淘汰候选";
            content = $"淘汰候选：{candidate.HerbBaseName} - {candidate.ProductName}，原因：{candidate.EliminationReason}";
        }
        else
        {
            throw new BusinessException(400, "候选状态仅支持待评估、保留、中选或淘汰");
        }

        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(opportunity.Id, actionType, content));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class UpdateCrmMatchCandidateCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public Guid CandidateId { get; set; }
    public decimal? AvailableQuantity { get; set; }
    public string Specification { get; set; } = string.Empty;
    public string QualityRequirement { get; set; } = string.Empty;
    public decimal? ExpectedPrice { get; set; }
    public string SupplyCycle { get; set; } = string.Empty;
    public DateTime? ValidUntil { get; set; }
}

public class UpdateCrmMatchCandidateHandler(ICrmDbContext dbContext) : IRequestHandler<UpdateCrmMatchCandidateCommand, bool>
{
    public async Task<bool> Handle(UpdateCrmMatchCandidateCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await CrmMatchOpportunityCommandGuards.FindOpportunityAsync(dbContext, request.OpportunityId, cancellationToken);
        CrmMatchOpportunityCommandGuards.EnsureInProgress(opportunity);
        var candidate = await dbContext.CrmMatchCandidates.FirstOrDefaultAsync(
            item => item.Id == request.CandidateId && item.MatchOpportunityId == opportunity.Id, cancellationToken)
            ?? throw new BusinessException(404, "撮合候选不存在");

        try
        {
            candidate.Update(
                request.AvailableQuantity,
                request.Specification,
                request.QualityRequirement,
                request.ExpectedPrice,
                request.SupplyCycle,
                request.ValidUntil);
        }
        catch (InvalidOperationException exception)
        {
            throw new BusinessException(400, exception.Message);
        }

        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(
            opportunity.Id,
            "修改候选",
            $"修改候选：{candidate.HerbBaseName} - {candidate.ProductName}；可供量：{candidate.AvailableQuantity}{candidate.QuantityUnit}；规格：{candidate.Specification}；预期价格：{candidate.ExpectedPrice}{candidate.PriceUnit}"));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class UpdateCrmMatchOpportunityDemandCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public Guid DemandId { get; set; }
    public decimal? Quantity { get; set; }
    public string Specification { get; set; } = string.Empty;
    public string QualityRequirement { get; set; } = string.Empty;
    public decimal? TargetPrice { get; set; }
    public DateTime? ExpectedDeliveryAt { get; set; }
}

public class UpdateCrmMatchOpportunityDemandHandler(ICrmDbContext dbContext) : IRequestHandler<UpdateCrmMatchOpportunityDemandCommand, bool>
{
    public async Task<bool> Handle(UpdateCrmMatchOpportunityDemandCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await CrmMatchOpportunityCommandGuards.FindOpportunityAsync(dbContext, request.OpportunityId, cancellationToken);
        CrmMatchOpportunityCommandGuards.EnsureInProgress(opportunity);
        var demand = await dbContext.CrmMatchOpportunityDemands.FirstOrDefaultAsync(
            item => item.Id == request.DemandId && item.OpportunityId == opportunity.Id, cancellationToken)
            ?? throw new BusinessException(404, "关联药厂需求不存在");
        try
        {
            demand.Update(request.Quantity, request.Specification, request.QualityRequirement, request.TargetPrice, request.ExpectedDeliveryAt);
        }
        catch (InvalidOperationException exception)
        {
            throw new BusinessException(400, exception.Message);
        }
        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(
            opportunity.Id,
            "修改候选用药厂需求",
            $"修改候选：{demand.VendorName} - {demand.ProductName}；需求数量：{demand.Quantity}{demand.QuantityUnit}；目标价：{demand.TargetPrice}{demand.PriceUnit}"));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class UpdateCrmMatchOpportunityDemandStatusCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public Guid DemandId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string EliminationReason { get; set; } = string.Empty;
}

public class UpdateCrmMatchOpportunityDemandStatusHandler(ICrmDbContext dbContext) : IRequestHandler<UpdateCrmMatchOpportunityDemandStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateCrmMatchOpportunityDemandStatusCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await CrmMatchOpportunityCommandGuards.FindOpportunityAsync(dbContext, request.OpportunityId, cancellationToken);
        CrmMatchOpportunityCommandGuards.EnsureInProgress(opportunity);
        var demand = await dbContext.CrmMatchOpportunityDemands.FirstOrDefaultAsync(
            item => item.Id == request.DemandId && item.OpportunityId == opportunity.Id, cancellationToken)
            ?? throw new BusinessException(404, "关联药厂需求不存在");

        var status = request.Status.Trim();
        // 前端统一按中文状态提交，药厂候选落库用英文枚举，这里做一次归一
        status = status switch
        {
            "保留" => CrmMatchOpportunityDemand.Kept,
            "中选" => CrmMatchOpportunityDemand.Selected,
            "淘汰" => CrmMatchOpportunityDemand.Eliminated,
            "待评估" => CrmMatchOpportunityDemand.PendingEvaluation,
            _ => status
        };
        string actionType;
        string content;
        if (status == CrmMatchOpportunityDemand.PendingEvaluation)
        {
            demand.ResetToPending();
            actionType = "恢复候选药厂";
            content = $"恢复候选药厂为待评估：{demand.VendorName} - {demand.ProductName}";
        }
        else if (status == CrmMatchOpportunityDemand.Kept)
        {
            demand.Keep();
            actionType = "保留候选药厂";
            content = $"保留候选药厂：{demand.VendorName} - {demand.ProductName}";
        }
        else if (status == CrmMatchOpportunityDemand.Selected)
        {
            demand.Select();
            actionType = "中选候选药厂";
            content = $"中选候选药厂：{demand.VendorName} - {demand.ProductName}";
        }
        else if (status == CrmMatchOpportunityDemand.Eliminated)
        {
            if (string.IsNullOrWhiteSpace(request.EliminationReason))
            {
                throw new BusinessException(400, "淘汰候选药厂必须填写淘汰原因");
            }

            demand.Eliminate(request.EliminationReason);
            actionType = "淘汰候选药厂";
            content = $"淘汰候选药厂：{demand.VendorName} - {demand.ProductName}，原因：{demand.EliminationReason}";
        }
        else
        {
            throw new BusinessException(400, "候选药厂状态仅支持待评估、保留、中选或淘汰");
        }

        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(opportunity.Id, actionType, content));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
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

            var selectedSupplyIds = await dbContext.CrmMatchCandidates
                .Where(item => item.MatchOpportunityId == opportunity.Id && item.Status == CrmMatchCandidate.Selected)
                .Select(item => item.HerbBaseSupplyId)
                .ToListAsync(cancellationToken);
            var selectedDemandCount = await dbContext.CrmMatchOpportunityDemands
                .CountAsync(
                    item => item.OpportunityId == opportunity.Id
                        && item.Role == CrmMatchOpportunityDemand.Candidate
                        && item.Status == CrmMatchOpportunityDemand.Selected,
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
                            dbContext,
                            supplyId,
                            opportunity.Id,
                            cancellationToken);
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
