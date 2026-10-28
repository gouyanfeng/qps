using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public enum MallArticleType
{
    HelpCenter,
    News,
    ActivityRule,
    UserGuide
}

public class MallArticle : BaseEntity
{
    public Guid StoreId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public string ContentHtml { get; private set; } = string.Empty;

    public MallArticleType ArticleType { get; private set; }

    public string Author { get; private set; } = string.Empty;

    public int ViewCount { get; private set; }

    public string SeoTitle { get; private set; } = string.Empty;

    public string SeoKeywords { get; private set; } = string.Empty;

    public string SeoDescription { get; private set; } = string.Empty;

    public bool IsTop { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    public bool IsActive { get; private set; }

    private MallArticle()
    {
    }

    private MallArticle(
        Guid storeId,
        string title,
        string summary,
        string contentHtml,
        MallArticleType articleType,
        string author,
        string seoTitle,
        string seoKeywords,
        string seoDescription,
        bool isTop,
        int sortOrder,
        bool isActive)
    {
        StoreId = storeId;
        Title = title;
        Summary = summary;
        ContentHtml = contentHtml;
        ArticleType = articleType;
        Author = author;
        SeoTitle = seoTitle;
        SeoKeywords = seoKeywords;
        SeoDescription = seoDescription;
        IsTop = isTop;
        SortOrder = sortOrder;
        IsActive = isActive;
        ViewCount = 0;
    }

    public static MallArticle Create(
        Guid storeId,
        string title,
        string summary,
        string contentHtml,
        MallArticleType articleType,
        string author,
        string seoTitle,
        string seoKeywords,
        string seoDescription,
        bool isTop,
        int sortOrder,
        bool isActive)
    {
        return new MallArticle(storeId, title, summary, contentHtml, articleType, author, seoTitle, seoKeywords, seoDescription, isTop, sortOrder, isActive);
    }

    public void Update(
        string title,
        string summary,
        string contentHtml,
        MallArticleType articleType,
        string author,
        string seoTitle,
        string seoKeywords,
        string seoDescription,
        int sortOrder)
    {
        Title = title;
        Summary = summary;
        ContentHtml = contentHtml;
        ArticleType = articleType;
        Author = author;
        SeoTitle = seoTitle;
        SeoKeywords = seoKeywords;
        SeoDescription = seoDescription;
        SortOrder = sortOrder;
    }

    public void SetTop(bool isTop)
    {
        IsTop = isTop;
    }

    public void Publish()
    {
        IsActive = true;
        PublishedAt = DateTime.UtcNow;
    }

    public void Unpublish()
    {
        IsActive = false;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
    }
}