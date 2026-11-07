using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

public class MallReturn : BaseEntity
{
    public string ReturnNo { get; private set; } = string.Empty;

    public Guid OrderId { get; private set; }

    public Guid PaymentId { get; private set; }

    public Guid? RefundId { get; private set; }

    public decimal Amount { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    /// <summary>补充说明，选择「其他」原因时填写。</summary>
    public string? Description { get; private set; }

    /// <summary>凭证图片相对地址，逗号分隔，最多 6 张。</summary>
    public string EvidenceImages { get; private set; } = string.Empty;

    /// <summary>审核拒绝原因。</summary>
    public string? RejectReason { get; private set; }

    public string Status { get; private set; } = MallReturnStatus.Pending;

    public string? ExpressCompany { get; private set; }

    public string? TrackingNo { get; private set; }

    public DateTime? ApprovedAt { get; private set; }

    public DateTime? RejectedAt { get; private set; }

    public DateTime? ShippedAt { get; private set; }

    public DateTime? ReceivedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    private MallReturn()
    {
    }

    private MallReturn(
        string returnNo,
        Guid orderId,
        Guid paymentId,
        decimal amount,
        string reason,
        string? description,
        string evidenceImages)
    {
        ReturnNo = returnNo;
        OrderId = orderId;
        PaymentId = paymentId;
        Amount = amount;
        Reason = reason;
        Description = description;
        EvidenceImages = evidenceImages;
        Status = MallReturnStatus.Pending;
    }

    public static MallReturn Create(
        string returnNo,
        Guid orderId,
        Guid paymentId,
        decimal amount,
        string reason,
        string? description = null,
        string evidenceImages = "")
    {
        return new MallReturn(
            returnNo,
            orderId,
            paymentId,
            amount,
            reason,
            description,
            evidenceImages);
    }

    public void Approve()
    {
        if (Status != MallReturnStatus.Pending)
            throw new BusinessException(400, "当前退货单状态不可审核通过");

        Status = MallReturnStatus.Approved;
        ApprovedAt = DateTime.Now;
    }

    public void Reject(string? rejectReason = null)
    {
        if (Status != MallReturnStatus.Pending)
            throw new BusinessException(400, "当前退货单状态不可审核拒绝");

        Status = MallReturnStatus.Rejected;
        RejectReason = string.IsNullOrWhiteSpace(rejectReason) ? null : rejectReason.Trim();
        RejectedAt = DateTime.Now;
    }

    public void SubmitTracking(string expressCompany, string trackingNo)
    {
        if (Status != MallReturnStatus.Approved)
            throw new BusinessException(400, "仅审核通过的退货单可填写寄回运单");

        ExpressCompany = expressCompany;
        TrackingNo = trackingNo;
        ShippedAt = DateTime.Now;
    }

    public void MarkReceived()
    {
        if (Status != MallReturnStatus.Approved)
            throw new BusinessException(400, "仅审核通过的退货单可标记已收货");

        Status = MallReturnStatus.Received;
        ReceivedAt = DateTime.Now;
    }

    public void MarkCompleted(Guid refundId)
    {
        if (Status != MallReturnStatus.Received)
            throw new BusinessException(400, "仅已收货的退货单可标记完成");

        Status = MallReturnStatus.Completed;
        RefundId = refundId;
        CompletedAt = DateTime.Now;
    }
}