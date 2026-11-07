namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 商城商品展示数据。
/// </summary>
public class MallProductDto
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string MainImageUrl { get; set; } = string.Empty;

    public List<string> AlbumImages { get; set; } = new();

    public string DetailHtml { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public bool IsHot { get; set; }

    public List<MallTagDto> Tags { get; set; } = new();

    public int Sales { get; set; }

    public Guid? FreightTemplateId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<MallProductPromotionDto> Promotions { get; set; } = new();

    /// <summary>商品图角标文案：命中满减促销时取促销标签名，无标签取促销名；为空表示不打角标。</summary>
    public string PromotionBadgeText { get; set; } = string.Empty;

    /// <summary>商品图角标颜色：取促销标签颜色，未配置时前端使用默认色。</summary>
    public string PromotionBadgeColor { get; set; } = string.Empty;

    public List<string> Images
    {
        get
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(MainImageUrl)) list.Add(MainImageUrl);
            if (AlbumImages != null) list.AddRange(AlbumImages);
            return list;
        }
    }

    public List<MallSkuDto> Skus { get; set; } = new();
}

/// <summary>
/// 商城商品规格展示数据。
/// </summary>
public class MallSkuDto
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string SkuCode { get; set; } = string.Empty;

    public string SpecName { get; set; } = string.Empty;

    /// <summary>售价。</summary>
    public decimal Price { get; set; }

    public decimal MarketPrice { get; set; }

    public int WeightGram { get; set; }

    public int VolumeCm3 { get; set; }

    public int Stock { get; set; }

    public int WarnStock { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<MallProductPromotionDto> Promotions { get; set; } = new();

    /// <summary>折后单价：命中折扣促销时为按最优折扣计算的价格，未命中为 null。</summary>
    public decimal? DiscountedPrice { get; set; }

    /// <summary>折扣标签文案，如「8折」；未命中折扣促销时为空。</summary>
    public string DiscountLabel { get; set; } = string.Empty;
}

public class MallProductPromotionDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>促销类型：0 满减 / 1 折扣 / 2 买赠。</summary>
    public int PromotionType { get; set; }

    /// <summary>订单门槛金额（单位：元）。</summary>
    public decimal ThresholdAmount { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string ActionConfigJson { get; set; } = "{}";

    public string ScopeType { get; set; } = string.Empty;

    public string RuleText { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

/// <summary>
/// 新增商品请求。
/// </summary>
public class MallProductCreateRequest
{
    public Guid StoreId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }

    public string MainImageUrl { get; set; } = string.Empty;

    public List<string> AlbumImages { get; set; } = new();

    public string DetailHtml { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsHot { get; set; }

    public List<Guid> TagIds { get; set; } = new();

    public Guid? FreightTemplateId { get; set; }
}

public class MallProductUpdateRequest
{
    public Guid StoreId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }

    public string MainImageUrl { get; set; } = string.Empty;

    public List<string> AlbumImages { get; set; } = new();

    public string DetailHtml { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsHot { get; set; }

    public List<Guid> TagIds { get; set; } = new();

    public Guid? FreightTemplateId { get; set; }
}

/// <summary>
/// 商品上下架请求。
/// </summary>
public class MallProductStatusRequest
{
    public bool IsActive { get; set; }
}

/// <summary>
/// 商品首页精选状态请求。
/// </summary>
public class MallProductHotRequest
{
    public bool IsHot { get; set; }
}

/// <summary>
/// 新增商品规格请求。
/// </summary>
public class MallSkuCreateRequest
{
    public string SkuCode { get; set; } = string.Empty;

    public string SpecName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal MarketPrice { get; set; }

    public int WeightGram { get; set; }

    public int VolumeCm3 { get; set; }

    public int Stock { get; set; }

    public int WarnStock { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 编辑商品规格请求。
/// </summary>
public class MallSkuUpdateRequest
{
    public string SkuCode { get; set; } = string.Empty;

    public string SpecName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal MarketPrice { get; set; }

    public int WeightGram { get; set; }

    public int VolumeCm3 { get; set; }

    public int Stock { get; set; }

    public int WarnStock { get; set; }
}

/// <summary>
/// 规格启用停用请求。
/// </summary>
public class MallSkuStatusRequest
{
    public bool IsActive { get; set; }
}