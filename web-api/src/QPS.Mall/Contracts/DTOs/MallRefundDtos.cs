namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 退款单列表项。
/// </summary>
public class MallRefundListDto
{
    public Guid Id { get; set; }

    public string RefundNo { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    public string OrderNo { get; set; } = string.Empty;

    public Guid PaymentId { get; set; }

    public string PaymentNo { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? RefundedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}