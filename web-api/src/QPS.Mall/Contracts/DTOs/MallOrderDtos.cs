using System.ComponentModel.DataAnnotations;

namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 商城订单列表项。
/// </summary>
public class MallOrderDto
{
    public Guid Id { get; set; }

    public string OrderNo { get; set; } = string.Empty;

    public Guid? MemberId { get; set; }

    /// <summary>会员昵称（无昵称时回退手机号），用于管理端订单列表展示。</summary>
    public string MemberName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    /// <summary>商品原价合计（促销与券抵扣前）。</summary>
    public decimal GoodsAmount { get; set; }

    /// <summary>促销优惠金额。</summary>
    public decimal PromotionDiscountAmount { get; set; }

    /// <summary>优惠券抵扣金额。</summary>
    public decimal CouponDiscountAmount { get; set; }

    /// <summary>优惠券 ID 快照（券后续被删除仍可追溯）。</summary>
    public Guid? CouponId { get; set; }

    /// <summary>优惠券名称快照。</summary>
    public string CouponName { get; set; } = string.Empty;

    public decimal FreightAmount { get; set; }

    /// <summary>应付金额 = max(0, 商品原价 - 促销优惠 - 券抵扣) + 运费。</summary>
    public decimal PayableAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal RefundedAmount { get; set; }

    public int TotalWeightGram { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    /// <summary>运单（一单一运单）；未发货时为空。</summary>
    public MallShipmentDto? Shipment { get; set; }

    public List<MallOrderItemDto> Items { get; set; } = new();
}

/// <summary>
/// 订单详情，一次装配明细、支付、退款与物流。
/// </summary>
public class MallOrderDetailDto : MallOrderDto
{
    public string ReceiverAddress { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    /// <summary>赠品未发放原因（库存不足等），无则为空。</summary>
    public string GiftShortageMessage { get; set; } = string.Empty;

    public DateTime? RefundedAt { get; set; }

    public List<MallPaymentDto> Payments { get; set; } = new();

    public List<MallRefundDto> Refunds { get; set; } = new();

    /// <summary>退货/售后单，供移动端展示售后进度。</summary>
    public List<MallReturnDto> Returns { get; set; } = new();
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
    public string ProductImage { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string SkuCode { get; set; } = string.Empty;
    public string SpecName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int WeightGram { get; set; }
    public decimal ItemAmount { get; set; }
    public bool IsGift { get; set; }
    public Guid? PromotionId { get; set; }

    /// <summary>主促销名称快照（不含金额拼接）。</summary>
    public string PromotionName { get; set; } = string.Empty;

    /// <summary>本行命中的促销快照 JSON：[{id, name, actionType, amount}]。</summary>
    public string PromotionSnapshot { get; set; } = "[]";

    /// <summary>本行实际促销优惠金额。</summary>
    public decimal PromotionDiscountAmount { get; set; }

    /// <summary>原价快照，恒有值；无促销时等于 UnitPrice。</summary>
    public decimal OriginalUnitPrice { get; set; }

    /// <summary>
    /// 该明细是否已发表评价。
    /// </summary>
    public bool Reviewed { get; set; }
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

    /// <summary>补充说明。</summary>
    public string? Description { get; set; }

    /// <summary>凭证图片。</summary>
    public string[] EvidenceImages { get; set; } = Array.Empty<string>();

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
    public string ExpressPhone { get; set; } = string.Empty;
    public string TrackingNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

/// <summary>
/// 创建订单请求（移动端下单）。
/// </summary>
public class CreateMallOrderRequest
{
    /// <summary>
    /// 下单会员ID，移动端JWT解析后自动注入。
    /// </summary>
    public Guid? MemberId { get; set; }

    /// <summary>
    /// 收货地址ID（会员地址簿），下单时按该地址快照收货信息。
    /// </summary>
    public Guid AddressId { get; set; }

    [MaxLength(500, ErrorMessage = "订单备注不能超过 500 个字符")]
    public string Remark { get; set; } = string.Empty;

    [Required(ErrorMessage = "请至少提交一个商品规格")]
    [MinLength(1, ErrorMessage = "请至少提交一个商品规格")]
    public List<CreateMallOrderItemRequest> Items { get; set; } = new();

    /// <summary>本次下单使用的会员券 ID，为空表示不使用优惠券。</summary>
    public Guid? MemberCouponId { get; set; }
}

/// <summary>
/// 下单商品明细。
/// </summary>
public class CreateMallOrderItemRequest
{
    public Guid SkuId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "商品数量必须大于 0")]
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
/// 申请退款请求。仅支持整单退款，退款金额由后端按订单实付金额计算。
/// </summary>
public class CreateMallRefundRequest
{
    /// <summary>申请原因，必填。</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>补充说明，选择「其他」原因时填写。</summary>
    public string? Description { get; set; }

    /// <summary>凭证图片相对地址，最多 6 张。</summary>
    public string[]? EvidenceImages { get; set; }

    /// <summary>
    /// 前端预期退款金额，仅用于一致性校验，不作为退款金额来源。
    /// 传入时必须等于订单实付金额，否则拒绝；不传则直接按订单实付金额退款。
    /// </summary>
    public decimal? Amount { get; set; }
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

/// <summary>
/// 订单确认页价格预览请求。
/// </summary>
public class PreviewMallOrderRequest
{
    [Required(ErrorMessage = "请选择要结算的商品")]
    [MinLength(1, ErrorMessage = "请选择要结算的商品")]
    public List<PreviewMallOrderItem> Items { get; set; } = new();

    /// <summary>要试算的会员券 ID，为空表示不使用优惠券。</summary>
    public Guid? MemberCouponId { get; set; }

    /// <summary>收货地址 ID，与下单保持一致且必填；预览按该地址计算运费。</summary>
    [Required(ErrorMessage = "请选择收货地址")]
    public Guid? AddressId { get; set; }
}

public class PreviewMallOrderItem
{
    public Guid SkuId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "商品数量必须大于 0")]
    public int Quantity { get; set; }
}

/// <summary>
/// 订单确认页价格预览结果。
/// </summary>
public class MallOrderPreviewDto
{
    /// <summary>商品原价合计。</summary>
    public decimal GoodsAmount { get; set; }

    /// <summary>促销优惠金额。</summary>
    public decimal PromotionDiscountAmount { get; set; }

    /// <summary>其中折后优惠（DiscountRate）金额。</summary>
    public decimal RateDiscountAmount { get; set; }

    /// <summary>其中满减优惠（DiscountAmount）金额。</summary>
    public decimal AmountDiscountAmount { get; set; }

    /// <summary>促销优惠明细。</summary>
    public List<MallOrderPromotionDto> PromotionDetails { get; set; } = new();

    /// <summary>优惠券抵扣金额。</summary>
    public decimal CouponDiscountAmount { get; set; }

    /// <summary>使用的优惠券名称（未使用券时为空）。</summary>
    public string? CouponName { get; set; }

    /// <summary>券因差额未达门槛时还差的金额，供结算页提示“还差 X 元”；其他不可用场景为 null。</summary>
    public decimal? CouponGapAmount { get; set; }

    /// <summary>券不可用原因提示（可用时为空）。</summary>
    public string? CouponHint { get; set; }

    /// <summary>运费金额（无地址、无模板或免邮时为 0）。</summary>
    public decimal FreightAmount { get; set; }

    /// <summary>应付金额 = max(0, GoodsAmount - PromotionDiscountAmount - CouponDiscountAmount) + FreightAmount。</summary>
    public decimal PayableAmount { get; set; }

    /// <summary>赠品清单（0 元，不参与金额计算，仅用于下单前展示）。</summary>
    public List<MallPromotionGiftDto> Gifts { get; set; } = new();
}

/// <summary>赠品展示项：商品信息 + 来源促销，0 元计价。</summary>
public class MallPromotionGiftDto
{
    public Guid SkuId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductImage { get; set; } = string.Empty;

    public string SpecName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public Guid PromotionId { get; set; }

    public string PromotionName { get; set; } = string.Empty;
}

public class MallOrderPromotionDto
{
    public Guid PromotionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal DiscountAmount { get; set; }
}