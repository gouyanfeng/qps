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

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 规格列表，仅详情查询返回。
    /// </summary>
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

    public int WeightGram { get; set; }

    public int Stock { get; set; }

    public int WarnStock { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
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
}

/// <summary>
/// 编辑商品请求。
/// </summary>
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
}

/// <summary>
/// 商品上下架请求。
/// </summary>
public class MallProductStatusRequest
{
    public bool IsActive { get; set; }
}

/// <summary>
/// 新增商品规格请求。
/// </summary>
public class MallSkuCreateRequest
{
    public string SkuCode { get; set; } = string.Empty;

    public string SpecName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int WeightGram { get; set; }

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

    public int WeightGram { get; set; }

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
