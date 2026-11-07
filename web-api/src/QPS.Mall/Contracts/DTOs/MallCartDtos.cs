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

    /// <summary>预计应付金额 = SelectedAmount - PromotionDiscountAmount。</summary>
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