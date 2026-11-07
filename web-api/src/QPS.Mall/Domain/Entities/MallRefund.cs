using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 退款单，整单退款，本地模拟退款网关。
/// </summary>
public class MallRefund : BaseEntity
{
    /// <summary>
    /// 退款单号，唯一。
    /// </summary>
    public string RefundNo { get; private set; } = string.Empty;

    /// <summary>
    /// 所属订单。
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// 对应的成功支付单。
    /// </summary>
    public Guid PaymentId { get; private set; }

    /// <summary>
    /// 退款金额。
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// 退款原因，申请时必填。
    /// </summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>
    /// 补充说明，选择「其他」原因时用于填写具体描述。
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// 凭证图片相对地址，逗号分隔，最多 6 张。
    /// </summary>
    public string EvidenceImages { get; private set; } = string.Empty;

    /// <summary>
    /// 退款状态，取值见 <see cref="MallRefundStatus"/>。
    /// </summary>
    public string Status { get; private set; } = MallRefundStatus.Pending;

    /// <summary>
    /// 退款成功时间。
    /// </summary>
    public DateTime? RefundedAt { get; private set; }

    private MallRefund()
    {
    }

    private MallRefund(
        string refundNo,
        Guid orderId,
        Guid paymentId,
        decimal amount,
        string reason,
        string? description,
        string evidenceImages)
    {
        RefundNo = refundNo;
        OrderId = orderId;
        PaymentId = paymentId;
        Amount = amount;
        Reason = reason;
        Description = description;
        EvidenceImages = evidenceImages;
        Status = MallRefundStatus.Pending;
    }

    public static MallRefund Create(
        string refundNo,
        Guid orderId,
        Guid paymentId,
        decimal amount,
        string reason,
        string? description = null,
        string evidenceImages = "")
    {
        return new MallRefund(refundNo, orderId, paymentId, amount, reason, description, evidenceImages);
    }

    /// <summary>
    /// 模拟退款成功。
    /// </summary>
    public void MarkSuccess()
    {
        if (Status != MallRefundStatus.Pending)
        {
            throw new BusinessException(400, "当前退款单状态不可处理");
        }

        Status = MallRefundStatus.Success;
        RefundedAt = DateTime.Now;
    }

    /// <summary>
    /// 模拟退款失败。
    /// </summary>
    public void MarkFailed()
    {
        if (Status != MallRefundStatus.Pending)
        {
            throw new BusinessException(400, "当前退款单状态不可处理");
        }

        Status = MallRefundStatus.Failed;
    }
}
