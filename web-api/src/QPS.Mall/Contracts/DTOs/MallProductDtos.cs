namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 商场商品展示数据。
/// </summary>
public class MallProductDto
{
    public Guid Id { get; set; }

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
/// 商场商品规格展示数据。
/// </summary>
public class MallSkuDto
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string SkuCode { get; set; } = string.Empty;

    public string SpecName { get; set; } = string.Empty;

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
}

public class MallProductPromotionDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string TriggerType { get; set; } = string.Empty;

    public string TriggerConfigJson { get; set; } = "{}";

    public string ActionType { get; set; } = string.Empty;

    public string ActionConfigJson { get; set; } = "{}";

    public string ScopeType { get; set; } = string.Empty;

    public string RuleText { get; set; } = string.Empty;

    public int Priority { get; set; }

    public bool Combinable { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

/// <summary>
/// 新增商品请求。
/// </summary>
public class MallProductCreateRequest
{
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