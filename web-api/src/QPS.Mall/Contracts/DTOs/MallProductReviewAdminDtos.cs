namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 后台商品评价列表数据。
/// </summary>
public class MallProductReviewAdminDto
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public Guid? MemberId { get; set; }

    public Guid? OrderId { get; set; }

    public string MemberNickname { get; set; } = string.Empty;

    public string MemberLevelName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Content { get; set; } = string.Empty;

    public string SpecText { get; set; } = string.Empty;

    public string[] Tags { get; set; } = Array.Empty<string>();

    public string[] Images { get; set; } = Array.Empty<string>();

    public bool IsFeatured { get; set; }

    public int SortOrder { get; set; }

    public int AuditStatus { get; set; }

    public string ReplyContent { get; set; } = string.Empty;

    public DateTime ReviewTime { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 评价审核状态变更请求。
/// </summary>
public class MallProductReviewAuditStatusRequest
{
    public int AuditStatus { get; set; }
}

/// <summary>
/// 评价回复请求，内容为空表示清除回复。
/// </summary>
public class MallProductReviewReplyRequest
{
    public string? ReplyContent { get; set; }
}

/// <summary>
/// 评价详情展示配置请求。
/// </summary>
public class MallProductReviewFeaturedRequest
{
    public bool IsFeatured { get; set; }

    public int SortOrder { get; set; }
}
