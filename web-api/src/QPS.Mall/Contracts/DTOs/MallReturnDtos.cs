namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 退货单统一视图：列表、详情以及创建/审核/发货/确认收货等命令共用同一套字段。
/// </summary>
public class MallReturnDto
{
    public Guid Id { get; set; }

    public string ReturnNo { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    public string OrderNo { get; set; } = string.Empty;

    public Guid PaymentId { get; set; }

    public Guid? RefundId { get; set; }

    /// <summary>退款金额。</summary>
    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? ExpressCompany { get; set; }

    public string? TrackingNo { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string UserPhone { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string SkuSpec { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string? Description { get; set; }

    /// <summary>凭证图片。</summary>
    public string[] EvidenceImages { get; set; } = Array.Empty<string>();

    public string? RejectReason { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
