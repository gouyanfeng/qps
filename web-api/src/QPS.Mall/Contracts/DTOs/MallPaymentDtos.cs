namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 支付单列表项。
/// </summary>
public class MallPaymentListDto
{
    public Guid Id { get; set; }

    public string PaymentNo { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    public string OrderNo { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Method { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string TransactionNo { get; set; } = string.Empty;

    public string FailureReason { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }
}