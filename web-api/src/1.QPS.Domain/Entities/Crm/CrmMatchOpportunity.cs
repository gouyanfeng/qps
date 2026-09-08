using QPS.Domain.Common;

namespace QPS.Domain.Entities.Crm;

public class CrmMatchOpportunity : BaseEntity
{
    public const string InProgress = "推进中";
    public const string Paused = "暂停";
    public const string Closed = "已关闭";
    public const string Matched = "撮合成功";
    public const string NotMatched = "未成交";
    public const string DemandCancelled = "需求取消";

    public Guid DemandItemId { get; private set; }
    public Guid DemandId { get; private set; }
    public Guid VendorId { get; private set; }
    public string DemandProductName { get; private set; } = string.Empty;
    public decimal? DemandQuantity { get; private set; }
    public string QuantityUnit { get; private set; } = string.Empty;
    public string Specification { get; private set; } = string.Empty;
    public string QualityRequirement { get; private set; } = string.Empty;
    public DateTime? ExpectedDeliveryAt { get; private set; }
    public string DeliveryAddress { get; private set; } = string.Empty;
    public Guid? OwnerUserId { get; private set; }
    public string Status { get; private set; } = InProgress;
    public string Stage { get; private set; } = CrmMatchOpportunityStage.MatchingVerification;
    public string NextAction { get; private set; } = string.Empty;
    public string NextActionRemark { get; private set; } = string.Empty;
    public DateTime? NextActionAt { get; private set; }
    public string CloseResult { get; private set; } = string.Empty;
    public string ClosedReason { get; private set; } = string.Empty;

    private CrmMatchOpportunity()
    {
    }

    public static CrmMatchOpportunity Create(
        Guid demandItemId,
        Guid demandId,
        Guid vendorId,
        string demandProductName,
        decimal? demandQuantity,
        string quantityUnit,
        string specification,
        string qualityRequirement,
        DateTime? expectedDeliveryAt,
        string deliveryAddress,
        Guid? ownerUserId,
        string nextAction,
        DateTime? nextActionAt,
        string nextActionRemark = "")
    {
        if (demandItemId == Guid.Empty)
        {
            throw new InvalidOperationException("撮合机会需求明细不能为空");
        }

        if (demandId == Guid.Empty)
        {
            throw new InvalidOperationException("撮合机会需求不能为空");
        }

        if (vendorId == Guid.Empty)
        {
            throw new InvalidOperationException("撮合机会需求厂商不能为空");
        }

        if (string.IsNullOrWhiteSpace(demandProductName))
        {
            throw new InvalidOperationException("撮合机会需求品类不能为空");
        }

        EnsureNextAction(nextAction, nextActionAt);

        return new CrmMatchOpportunity
        {
            DemandItemId = demandItemId,
            DemandId = demandId,
            VendorId = vendorId,
            DemandProductName = Trim(demandProductName),
            DemandQuantity = demandQuantity,
            QuantityUnit = Trim(quantityUnit),
            Specification = Trim(specification),
            QualityRequirement = Trim(qualityRequirement),
            ExpectedDeliveryAt = expectedDeliveryAt,
            DeliveryAddress = Trim(deliveryAddress),
            OwnerUserId = ownerUserId,
            Status = InProgress,
            Stage = CrmMatchOpportunityStage.MatchingVerification,
            NextAction = Trim(nextAction),
            NextActionRemark = Trim(nextActionRemark),
            NextActionAt = nextActionAt
        };
    }

    public void Advance(string stage, string nextAction, string nextActionRemark, DateTime? nextActionAt)
    {
        EnsureCanUpdate();

        if (!CrmMatchOpportunityStage.IsValid(stage) || !CrmMatchOpportunityStage.CanTransit(Stage, stage))
        {
            throw new InvalidOperationException("撮合阶段仅允许保持当前阶段或相邻跳转");
        }

        CrmMatchOpportunityNextAction.Validate(nextAction, nextActionRemark);
        EnsureNextAction(nextAction, nextActionAt);
        Stage = Trim(stage);
        NextAction = Trim(nextAction);
        NextActionRemark = Trim(nextActionRemark);
        NextActionAt = nextActionAt;
    }

    public void UpdateNextAction(string nextAction, DateTime? nextActionAt)
    {
        EnsureCanUpdate();
        EnsureNextAction(nextAction, nextActionAt);
        NextAction = Trim(nextAction);
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
