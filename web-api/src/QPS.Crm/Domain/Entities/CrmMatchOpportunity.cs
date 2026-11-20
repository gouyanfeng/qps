using QPS.Shared.Common;

namespace QPS.Crm.Domain.Entities;

/// <summary>
/// 撮合机会主表：只存一次撮合的流程数据。
/// 发起方与候选统一放在 CrmMatchOpportunityItem 子表，业务字段不存副本。
/// </summary>
public class CrmMatchOpportunity : BaseEntity
{
    public const string InProgress = "推进中";
    public const string Paused = "暂停";
    public const string Closed = "已关闭";
    public const string Matched = "撮合成功";
    public const string NotMatched = "未成交";
    public const string DemandCancelled = "需求取消";

    public Guid? OwnerUserId { get; private set; }
    public string Status { get; private set; } = InProgress;
    public string Stage { get; private set; } = CrmMatchOpportunityStage.MatchingVerification;
    public string NextAction { get; private set; } = string.Empty;
    public string NextActionRemark { get; private set; } = string.Empty;
    public DateTime? NextActionAt { get; private set; }
    public string CloseResult { get; private set; } = string.Empty;
    public string ClosedReason { get; private set; } = string.Empty;
    /// <summary>发起方向：demand=采购列表发起，supply=供应列表发起。</summary>
    public string Source { get; private set; } = "demand";

    private CrmMatchOpportunity()
    {
    }

    public static CrmMatchOpportunity Create(
        Guid? ownerUserId,
        string nextAction,
        DateTime? nextActionAt,
        string nextActionRemark = "",
        string source = "demand")
    {
        if (source != "supply" && source != "demand")
        {
            throw new InvalidOperationException("撮合发起方向无效");
        }

        EnsureNextAction(nextAction, nextActionAt);

        return new CrmMatchOpportunity
        {
            OwnerUserId = ownerUserId,
            Status = InProgress,
            Stage = CrmMatchOpportunityStage.MatchingVerification,
            NextAction = Trim(nextAction),
            NextActionRemark = Trim(nextActionRemark),
            NextActionAt = nextActionAt,
            Source = source
        };
    }

    public void Advance(string stage, string nextAction, string nextActionRemark, DateTime? nextActionAt)
    {
        EnsureCanUpdate();

        if (!CrmMatchOpportunityStage.IsValid(stage) || !CrmMatchOpportunityStage.CanTransit(Stage, stage))
        {
            throw new InvalidOperationException("撮合阶段仅允许保持当前阶段或相邻跳转");
        }

        CrmMatchOpportunityNextAction.Validate(stage, nextAction, nextActionRemark);
        EnsureNextAction(nextAction, nextActionAt);
        Stage = Trim(stage);
        NextAction = Trim(nextAction);
        NextActionRemark = Trim(nextActionRemark);
        NextActionAt = nextActionAt;
    }

    public void Pause()
    {
        if (Status != InProgress)
        {
            throw new InvalidOperationException("仅推进中的撮合机会可以暂停");
        }

        Status = Paused;
    }

    public void Resume()
    {
        if (Status != Paused)
        {
            throw new InvalidOperationException("仅暂停的撮合机会可以恢复");
        }

        Status = InProgress;
    }

    public void Close(string closeResult, string closedReason)
    {
        EnsureCanUpdate();

        if (closeResult != Matched && closeResult != NotMatched && closeResult != DemandCancelled)
        {
            throw new InvalidOperationException("撮合机会关闭结果无效");
        }

        if (closeResult != Matched && string.IsNullOrWhiteSpace(closedReason))
        {
            throw new InvalidOperationException("未成交或需求取消必须填写关闭原因");
        }

        Status = Closed;
        CloseResult = closeResult;
        ClosedReason = Trim(closedReason);
    }

    private void EnsureCanUpdate()
    {
        if (Status == Closed)
        {
            throw new InvalidOperationException("已关闭撮合机会不可编辑");
        }
    }

    private static void EnsureNextAction(string nextAction, DateTime? nextActionAt)
    {
        if (string.IsNullOrWhiteSpace(nextAction) || !nextActionAt.HasValue)
        {
            throw new InvalidOperationException("下一步动作和计划时间必须同时填写");
        }
    }

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;
}
