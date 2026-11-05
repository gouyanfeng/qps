namespace QPS.Mall.Contracts.DTOs;

public class MallCartDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public int TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }

    /// <summary>当前选中商品的原价合计。</summary>
    public decimal SelectedAmount { get; set; }

    /// <summary>预计促销优惠金额（按购物车选中且有效的商品与数量试算，最终以下单结果为准）。</summary>
    public decimal PromotionDiscountAmount { get; set; }

    /// <summary>预计应付金额 = SelectedAmount - PromotionDiscountAmount。</summary>
    public decimal PayableAmount { get; set; }

    /// <summary>预计促销优惠明细（逐条促销的优惠金额）。</summary>
    public List<MallCartPromotionDto> PromotionDetails { get; set; } = new();

    public DateTime LastActiveAt { get; set; }

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