namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 活动列表项，包含关联促销的精简信息。
/// </summary>
public class MallActivityListItemDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string CoverImage { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<MallActivityPromotionDto> Promotions { get; set; } = new();
}

/// <summary>
/// 活动详情，包含富文本详情。
/// </summary>
public class MallActivityDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string CoverImage { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string DetailHtml { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<MallActivityPromotionDto> Promotions { get; set; } = new();
}

/// <summary>
/// 活动关联的促销摘要。
/// </summary>
public class MallActivityPromotionDto
{
    public Guid Id { get; set; }

    public Guid PromotionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string TriggerType { get; set; } = string.Empty;

    public string ActionType { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }
}

public class MallActivityCreateRequest
{
    public string Name { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string CoverImage { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string DetailHtml { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public List<Guid> PromotionIds { get; set; } = new();
}

public class MallActivityUpdateRequest
{
    public string Name { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string CoverImage { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string DetailHtml { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int SortOrder { get; set; }

    public List<Guid> PromotionIds { get; set; } = new();
}

public class MallActivityStatusRequest
{
    public bool IsActive { get; set; }
}
