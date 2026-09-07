using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Attributes;
using QPS.Application.Features.System;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmMatchOpportunities;

[Permission(SystemPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class UpdateCrmMatchCandidateStatusCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public Guid CandidateId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string EliminationReason { get; set; } = string.Empty;
}

public class UpdateCrmMatchCandidateStatusHandler(IDbContext dbContext) : IRequestHandler<UpdateCrmMatchCandidateStatusCommand, bool>
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
        if (status == CrmMatchCandidate.Kept)
        {
            candidate.Keep();
            actionType = "保留候选";
            content = $"保留候选：{candidate.HerbBaseName} - {candidate.ProductName}";
        }
        else if (status == CrmMatchCandidate.Selected)
        {
            await CrmMatchOpportunitySource.LoadEligibleSuppliesAsync(dbContext, [candidate.HerbBaseSupplyId], cancellationToken);
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
            throw new BusinessException(400, "候选状态仅支持保留、中选或淘汰");
        }

        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(opportunity.Id, actionType, content));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

[Permission(SystemPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class UpdateCrmMatchOpportunityNextActionCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public string NextAction { get; set; } = string.Empty;
    public DateTime? NextActionAt { get; set; }
}

public class UpdateCrmMatchOpportunityNextActionHandler(IDbContext dbContext) : IRequestHandler<UpdateCrmMatchOpportunityNextActionCommand, bool>
{
    public async Task<bool> Handle(UpdateCrmMatchOpportunityNextActionCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await CrmMatchOpportunityCommandGuards.FindOpportunityAsync(dbContext, request.OpportunityId, cancellationToken);
        if (opportunity.Status == CrmMatchOpportunity.Closed)
        {
            throw new BusinessException(400, "已关闭撮合机会不可编辑");
        }

        CrmMatchOpportunitySource.ValidateNextAction(request.NextAction, request.NextActionAt);
        opportunity.UpdateNextAction(request.NextAction, request.NextActionAt);
        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(opportunity.Id, "更新下一步", $"更新下一步：{opportunity.NextAction}，计划时间：{request.NextActionAt!.Value:yyyy-MM-dd HH:mm}"));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

[Permission(SystemPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class UpdateCrmMatchOpportunityStatusCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CloseResult { get; set; } = string.Empty;
    public string ClosedReason { get; set; } = string.Empty;
    public string NextAction { get; set; } = string.Empty;
    public DateTime? NextActionAt { get; set; }
}

public class UpdateCrmMatchOpportunityStatusHandler(IDbContext dbContext) : IRequestHandler<UpdateCrmMatchOpportunityStatusCommand, bool>
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
        else if (status == CrmMatchOpportunity.InProgress)
        {
            if (opportunity.Status != CrmMatchOpportunity.Paused)
            {
                throw new BusinessException(400, "仅暂停的撮合机会可以恢复");
            }

            if (opportunity.NextActionAt < DateTime.Now)
            {
                CrmMatchOpportunitySource.ValidateNextAction(request.NextAction, request.NextActionAt);
                opportunity.UpdateNextAction(request.NextAction, request.NextActionAt);
            }

            opportunity.Resume();
            actionType = "恢复";
            content = opportunity.NextActionAt == request.NextActionAt
                ? $"恢复撮合机会，并重新安排下一步：{opportunity.NextAction}，计划时间：{opportunity.NextActionAt:yyyy-MM-dd HH:mm}"
                : "恢复撮合机会";
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

            var selectedSupplyIds = await dbContext.CrmMatchCandidates
                .Where(item => item.MatchOpportunityId == opportunity.Id && item.Status == CrmMatchCandidate.Selected)
                .Select(item => item.HerbBaseSupplyId)
                .ToListAsync(cancellationToken);
            if (closeResult == CrmMatchOpportunity.Matched)
            {
                if (selectedSupplyIds.Count == 0)
                {
                    throw new BusinessException(400, "撮合成功必须至少中选一条候选供应信息");
                }

                await CrmMatchOpportunitySource.LoadEligibleSuppliesAsync(dbContext, selectedSupplyIds, cancellationToken);
            }
            else if (selectedSupplyIds.Count > 0)
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
    public static async Task<CrmMatchOpportunity> FindOpportunityAsync(IDbContext dbContext, Guid opportunityId, CancellationToken cancellationToken) =>
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
