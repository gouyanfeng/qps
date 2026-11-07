namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 发货单（运单）列表项。
/// </summary>
public class MallShipmentListDto
{
    public Guid Id { get; set; }

    public string ShipmentNo { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    public string OrderNo { get; set; } = string.Empty;

    public string ExpressCompany { get; set; } = string.Empty;

    public string TrackingNo { get; set; } = string.Empty;

    /// <summary>运单状态，已作废的记录同样保留。</summary>
    public string Status { get; set; } = string.Empty;

    public bool IsVoided { get; set; }

    public string Remark { get; set; } = string.Empty;

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? VoidedAt { get; set; }

    public string VoidedBy { get; set; } = string.Empty;

    public string VoidReason { get; set; } = string.Empty;
}

/// <summary>
/// 发货单详情，补充订单与收货信息供抽屉展示。
/// </summary>
public class MallShipmentDetailDto : MallShipmentListDto
{
    public string OrderStatus { get; set; } = string.Empty;

    public string MemberName { get; set; } = string.Empty;

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public decimal PaidAmount { get; set; }

    /// <summary>商品件数（含赠品）。</summary>
    public int ItemCount { get; set; }
}

/// <summary>
/// 作废运单请求。
/// </summary>
public class VoidMallShipmentRequest
{
    /// <summary>作废原因，必填。</summary>
    public string Reason { get; set; } = string.Empty;
}
