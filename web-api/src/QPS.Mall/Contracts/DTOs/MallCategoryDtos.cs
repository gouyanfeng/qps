namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 商品分类展示数据。
/// </summary>
public class MallCategoryDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 新增商品分类请求。
/// </summary>
public class MallCategoryCreateRequest
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 编辑商品分类请求。
/// </summary>
public class MallCategoryUpdateRequest
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

/// <summary>
/// 分类启用停用请求。
/// </summary>
public class MallCategoryStatusRequest
{
    public bool IsActive { get; set; }
}
