namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 商场订单列表项。
/// </summary>
public class MallOrderDto
{
    public Guid Id { get; set; }

    public string OrderNo { get; set; } = string.Empty;

    public Guid StoreId { get; set; }

    public Guid? MemberId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public decimal GoodsAmount { get; set; }

    public decimal PromotionDiscountAmount { get; set; }

    public decimal FreightAmount { get; set; }

    public decimal PayableAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal RefundedAmount { get; set; }

    public int TotalWeightGram { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }
}

/// <summary>
/// 订单详情，一次装配明细、支付、退款与物流。
/// </summary>
public class MallOrderDetailDto : MallOrderDto
{
    public string ReceiverAddress { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public DateTime? RefundedAt { get; set; }

    public List<MallOrderItemDto> Items { get; set; } = new();

    public List<MallPaymentDto> Payments { get; set; } = new();

    public List<MallRefundDto> Refunds { get; set; } = new();

    public MallShipmentDto? Shipment { get; set; }
}

/// <summary>
/// 订单明细。
/// </summary>
public class MallOrderItemDto
{
    public Guid Id { get; set; }

    public Guid SkuId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SkuCode { get; set; } = string.Empty;

    public string SpecName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public int WeightGram { get; set; }

    public decimal ItemAmount { get; set; }

    public bool IsGift { get; set; }

    public Guid? PromotionId { get; set; }

    public string PromotionName { get; set; } = string.Empty;

    public decimal? OriginalUnitPrice { get; set; }
}

/// <summary>
/// 支付单。
/// </summary>
public class MallPaymentDto
{
    public Guid Id { get; set; }

    public string PaymentNo { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Method { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string TransactionNo { get; set; } = string.Empty;

    public string FailureReason { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 退款单。
/// </summary>
public class MallRefundDto
{
    public Guid Id { get; set; }

    public string RefundNo { get; set; } = string.Empty;

    public Guid PaymentId { get; set; }

    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? RefundedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 运单。
/// </summary>
public class MallShipmentDto
{
    public Guid Id { get; set; }

    public string ShipmentNo { get; set; } = string.Empty;

    public string ExpressCompany { get; set; } = string.Empty;

    public string TrackingNo { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }
}

/// <summary>
/// 外部下单请求。
/// </summary>
public class CreateMallOrderRequest
{
    public Guid StoreId { get; set; }

    /// <summary>
    /// 下单会员ID，移动端JWT解析后自动注入，后台代下单可空。
    /// </summary>
    public Guid? MemberId { get; set; }

    /// <summary>
    /// 外部订单号，为空时由系统生成。
    /// </summary>
    public string? OrderNo { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string ProvinceCode { get; set; } = string.Empty;

    public string ProvinceName { get; set; } = string.Empty;

    public string CityCode { get; set; } = string.Empty;

    public string CityName { get; set; } = string.Empty;

    public string DistrictCode { get; set; } = string.Empty;

    public string DistrictName { get; set; } = string.Empty;

    public string ReceiverAddress { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public List<CreateMallOrderItemRequest> Items { get; set; } = new();
}

/// <summary>
/// 外部下单明细。
/// </summary>
public class CreateMallOrderItemRequest
{
    public Guid SkuId { get; set; }

    public int Quantity { get; set; }
}

/// <summary>
/// 创建支付单请求。
/// </summary>
public class CreateMallPaymentRequest
{
    /// <summary>
    /// 支付方式，默认 Mock。
    /// </summary>
    public string Method { get; set; } = "Mock";
}

/// <summary>
/// 模拟支付结果请求。
/// </summary>
public class SimulateMallPaymentRequest
{
    public bool Success { get; set; }

    public string? FailureReason { get; set; }
}

/// <summary>
/// 申请退款请求。
/// </summary>
public class CreateMallRefundRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 模拟退款结果请求。
/// </summary>
public class SimulateMallRefundRequest
{
    public bool Success { get; set; }
}

/// <summary>
/// 发货请求。
/// </summary>
public class CreateMallShipmentRequest
{
    public string ExpressCompany { get; set; } = string.Empty;

    public string TrackingNo { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;
}