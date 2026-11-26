using System.ComponentModel.DataAnnotations;

namespace QPS.Mall.Contracts.DTOs;

public class MallCartDto
{
    public int TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }

    /// <summary>当前选中商品的原价合计。</summary>
    public decimal SelectedAmount { get; set; }

    /// <summary>预计促销优惠金额（按购物车选中且有效的商品与数量试算，最终以下单结果为准）。</summary>
    public decimal PromotionDiscountAmount { get; set; }

    /// <summary>其中折后优惠（DiscountRate）金额。</summary>
    public decimal RateDiscountAmount { get; set; }

    /// <summary>其中满减优惠（DiscountAmount）金额。</summary>
    public decimal AmountDiscountAmount { get; set; }

    /// <summary>优惠券抵扣金额；购物车阶段（未选券）为 0。</summary>
    public decimal CouponDiscountAmount { get; set; }

    /// <summary>使用的优惠券名称（未使用券时为空）。</summary>
    public string? CouponName { get; set; }

    /// <summary>券因差额未达门槛时还差的金额，其他不可用场景为 null。</summary>
    public decimal? CouponGapAmount { get; set; }

    /// <summary>券不可用原因提示（可用时为空）。</summary>
    public string? CouponHint { get; set; }

    /// <summary>运费金额；购物车阶段（无地址）为 0。</summary>
    public decimal FreightAmount { get; set; }

    /// <summary>折后商品额 = max(0, SelectedAmount - 促销 - 券)。</summary>
    public decimal DiscountedGoodsAmount { get; set; }

    /// <summary>
    /// 预计应付金额 = max(0, SelectedAmount - 促销 - 券) + 运费。
    /// 传地址/券后与结算页口径完全一致；购物车阶段两者为 0。
    /// </summary>
    public decimal PayableAmount { get; set; }

    /// <summary>预计促销优惠明细（逐条促销的优惠金额）。</summary>
    public List<MallCartPromotionDto> PromotionDetails { get; set; } = new();

    /// <summary>按当前选中商品试算命中的赠品（0 元，不参与金额，仅用于展示）。</summary>
    public List<MallPromotionGiftDto> Gifts { get; set; } = new();

    public List<MallCartItemDto> Items { get; set; } = new();
}

public class MallCartPromotionDto
{
    public Guid PromotionId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>该促销带来的优惠金额。</summary>
    public decimal DiscountAmount { get; set; }
}

public class MallCartItemDto
{
    public Guid Id { get; set; }

    public Guid SkuId { get; set; }

    public Guid ProductId { get; set; }

    public Guid StoreId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string ProductImage { get; set; } = string.Empty;

    public string SkuCode { get; set; } = string.Empty;

    public string SpecName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    /// <summary>折后价（与商品列表同一套计算；无折扣时等于 Price）。</summary>
    public decimal DiscountedPrice { get; set; }

    /// <summary>折扣标签文案（如“8折”），无折扣时为空。</summary>
    public string? DiscountLabel { get; set; }

    /// <summary>满减促销角标文案（与商品列表同一套判定），无生效满减时为空。</summary>
    public string? PromotionBadgeText { get; set; }

    /// <summary>满减促销角标背景色（取自促销标签，可为空）。</summary>
    public string? PromotionBadgeColor { get; set; }

    public int Stock { get; set; }

    public int WeightGram { get; set; }

    public int AvailableStock { get; set; }

    public decimal LineAmount { get; set; }

    public bool IsValid { get; set; } = true;

    public string? InvalidReason { get; set; }
}

/// <summary>临时试算的一行商品：用 skuId + 数量复用购物车组装与试算，不写入购物车库。</summary>
public class CartPreviewLine
{
    [Required(ErrorMessage = "请指定商品规格")]
    public Guid SkuId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "数量必须大于 0")]
    public int Quantity { get; set; }
}

/// <summary>临时行试算请求：不写购物车，仅按传入的规格与数量预览商品价格与促销。</summary>
public class MallCartPreviewRequest
{
    [Required(ErrorMessage = "请指定要试算的商品")]
    [MinLength(1, ErrorMessage = "请指定要试算的商品")]
    public List<CartPreviewLine> Items { get; set; } = new();

    /// <summary>收货地址 ID；结算页传入以计算运费，纯商品预览可不传。</summary>
    public Guid? AddressId { get; set; }

    /// <summary>会员券 ID；结算页传入以计算券抵扣。</summary>
    public Guid? MemberCouponId { get; set; }
}

public class MallCartAddRequest
{
    public Guid UserId { get; set; }

    public Guid SkuId { get; set; }

    public int Quantity { get; set; }
}

public class MallCartUpdateItemRequest
{
    public Guid UserId { get; set; }

    public Guid SkuId { get; set; }

    public int Quantity { get; set; }
}

/// <summary>按下单明细批量移除购物车商品，用于下单成功后只清理本次提交的规格。</summary>
public class MallCartRemoveItemsRequest
{
    [Required(ErrorMessage = "请指定要移除的商品规格")]
    [MinLength(1, ErrorMessage = "请指定要移除的商品规格")]
    public List<Guid> SkuIds { get; set; } = new();
}