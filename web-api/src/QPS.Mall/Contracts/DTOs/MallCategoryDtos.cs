namespace QPS.Mall.Contracts.DTOs;

public class MallCategoryDto
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }

    public string ParentName { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class MallCategoryCreateRequest
{
    public Guid StoreId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }

    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class MallCategoryUpdateRequest
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }

    public string? Icon { get; set; }

    public int SortOrder { get; set; }
}

public class MallCategoryStatusRequest
{
    public bool IsActive { get; set; }
}