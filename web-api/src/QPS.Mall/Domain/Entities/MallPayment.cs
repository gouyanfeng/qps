using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 支付单，本地模拟支付网关。
/// </summary>
public class MallPayment : BaseEntity
{
    /// <summary>
    /// 支付单号，唯一。
    /// </summary>
    public string PaymentNo { get; private set; } = string.Empty;

    /// <summary>
    /// 所属订单。
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// 支付金额。
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// 支付方式，例如 Mock。
    /// </summary>
    public string Method { get; private set; } = string.Empty;

    /// <summary>
    /// 支付状态，取值见 <see cref="MallPaymentStatus"/>。
    /// </summary>
    public string Status { get; private set; } = MallPaymentStatus.Pending;

    /// <summary>
    /// 第三方交易号。
    /// </summary>
    public string TransactionNo { get; private set; } = string.Empty;

    /// <summary>
    /// 失败原因。
    /// </summary>
    public string FailureReason { get; private set; } = string.Empty;

    /// <summary>
    /// 支付成功时间。
    /// </summary>
    public DateTime? PaidAt { get; private set; }

    private MallPayment()
    {
    }

    private MallPayment(string paymentNo, Guid orderId, decimal amount, string method)
    {
        PaymentNo = paymentNo;
        OrderId = orderId;
        Amount = amount;
        Method = method;
        Status = MallPaymentStatus.Pending;
    }

    public static MallPayment Create(string paymentNo, Guid orderId, decimal amount, string method)
    {
        return new MallPayment(paymentNo, orderId, amount, method);
    }

    /// <summary>
    /// 模拟支付成功。
    /// </summary>
    public void MarkSuccess(string transactionNo)
    {
        if (Status != MallPaymentStatus.Pending)
        {
            throw new BusinessException(400, "当前支付单状态不可支付");
        }

        Status = MallPaymentStatus.Success;
        TransactionNo = transactionNo;
        PaidAt = DateTime.Now;
    }

    /// <summary>
    /// 模拟支付失败。
    /// </summary>
    public void MarkFailed(string failureReason)
    {
        if (Status != MallPaymentStatus.Pending)
        {
            throw new BusinessException(400, "当前支付单状态不可支付");
        }

        Status = MallPaymentStatus.Failed;
        FailureReason = failureReason;
    }

    /// <summary>
    /// 订单取消时关闭未决支付单。
    /// </summary>
    public void Cancel()
    {
        if (Status != MallPaymentStatus.Pending)
        {
            throw new BusinessException(400, "当前支付单状态不可关闭");
        }

        Status = MallPaymentStatus.Cancelled;
    }

    /// <summary>
    /// 整单退款成功后同步支付单状态。
    /// </summary>
    public void MarkRefunded()
    {
        if (Status != MallPaymentStatus.Success)
        {
            throw new BusinessException(400, "仅支付成功的支付单可以标记退款");
        }

        Status = MallPaymentStatus.Refunded;
    }
}
