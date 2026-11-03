namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 商品评价展示数据。
/// </summary>
public class MallProductReviewDto
{
    public Guid Id { get; set; }

    public string MemberNickname { get; set; } = string.Empty;

    public string MemberLevelName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Content { get; set; } = string.Empty;

    public string SpecText { get; set; } = string.Empty;

    public string[] Tags { get; set; } = Array.Empty<string>();

    public string[] Images { get; set; } = Array.Empty<string>();

    public string ReplyContent { get; set; } = string.Empty;

    public bool IsFeatured { get; set; }

    public int SortOrder { get; set; }

    public DateTime ReviewTime { get; set; }
}

/// <summary>
/// 会员发表评价请求。
/// </summary>
public class MallProductReviewCreateRequest
{
    public string OrderNo { get; set; } = string.Empty;

    public Guid OrderItemId { get; set; }

    public int Rating { get; set; }

    public string Content { get; set; } = string.Empty;

    public string[] Tags { get; set; } = Array.Empty<string>();

    public string[] Images { get; set; } = Array.Empty<string>();
}

/// <summary>
/// 商品评价标签统计。
/// </summary>
public class MallProductReviewTagStatDto
{
    public string Tag { get; set; } = string.Empty;

    public int Count { get; set; }
}

/// <summary>
/// 商品评价分页数据（含好评率与标签统计）。
/// </summary>
public class MallProductReviewPageDto
{
    public List<MallProductReviewDto> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public decimal PositiveRate { get; set; }

    /// <summary>
    /// 该商品全部已通过评价数量，不受标签与精选筛选影响。
    /// </summary>
    public int AllTotalCount { get; set; }

    /// <summary>
    /// 该商品全部已通过评价的好评率，不受标签与精选筛选影响。
    /// </summary>
    public decimal AllPositiveRate { get; set; }

    public List<MallProductReviewTagStatDto> TagStats { get; set; } = new();
}
