using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Contracts.DTOs;

// ===== Banner =====

public class MallBannerDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Position { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public MallBannerLinkType LinkType { get; set; }
    public string LinkValue { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MallBannerCreateRequest
{
    public Guid StoreId { get; set; }
    public string Position { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public MallBannerLinkType LinkType { get; set; }
    public string LinkValue { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MallBannerUpdateRequest
{
    public string Position { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public MallBannerLinkType LinkType { get; set; }
    public string LinkValue { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class MallBannerBatchSortRequest
{
    public List<MallBannerSortItem> Items { get; set; } = new();
}

public class MallBannerSortItem
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
}

// ===== Notice =====

public class MallNoticeDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MallNoticeType NoticeType { get; set; }
    public bool IsTop { get; set; }
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MallNoticeCreateRequest
{
    public Guid StoreId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MallNoticeType NoticeType { get; set; }
    public bool IsTop { get; set; }
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MallNoticeUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MallNoticeType NoticeType { get; set; }
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

// ===== Article =====

public class MallArticleDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public MallArticleType ArticleType { get; set; }
    public string Author { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoKeywords { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
    public bool IsTop { get; set; }
    public int SortOrder { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MallArticleListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public MallArticleType ArticleType { get; set; }
    public string Author { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public bool IsTop { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MallArticleCreateRequest
{
    public Guid StoreId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public MallArticleType ArticleType { get; set; }
    public string Author { get; set; } = string.Empty;
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoKeywords { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
    public bool IsTop { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = false;
}

public class MallArticleUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public MallArticleType ArticleType { get; set; }
    public string Author { get; set; } = string.Empty;
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoKeywords { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

// ===== Page =====

public class MallPageDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string PageCode { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoKeywords { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MallPageCreateRequest
{
    public Guid StoreId { get; set; }
    public string PageCode { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoKeywords { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class MallPageUpdateRequest
{
    public string PageName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoKeywords { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
}