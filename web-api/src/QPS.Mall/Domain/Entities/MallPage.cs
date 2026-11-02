using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public class MallPage : BaseEntity
{
    public string PageCode { get; private set; } = string.Empty;

    public string PageName { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string ContentHtml { get; private set; } = string.Empty;

    public string SeoTitle { get; private set; } = string.Empty;

    public string SeoKeywords { get; private set; } = string.Empty;

    public string SeoDescription { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    private MallPage()
    {
    }

    private MallPage(
        string pageCode,
        string pageName,
        string title,
        string contentHtml,
        string seoTitle,
        string seoKeywords,
        string seoDescription,
        bool isActive)
    {
        PageCode = pageCode;
        PageName = pageName;
        Title = title;
        ContentHtml = contentHtml;
        SeoTitle = seoTitle;
        SeoKeywords = seoKeywords;
        SeoDescription = seoDescription;
        IsActive = isActive;
    }

    public static MallPage Create(
        string pageCode,
        string pageName,
        string title,
        string contentHtml,
        string seoTitle,
        string seoKeywords,
        string seoDescription,
        bool isActive)
    {
        return new MallPage(pageCode, pageName, title, contentHtml, seoTitle, seoKeywords, seoDescription, isActive);
    }

    public void Update(
        string pageName,
        string title,
        string contentHtml,
        string seoTitle,
        string seoKeywords,
        string seoDescription)
    {
        PageName = pageName;
        Title = title;
        ContentHtml = contentHtml;
        SeoTitle = seoTitle;
        SeoKeywords = seoKeywords;
        SeoDescription = seoDescription;
    }

    public void ChangeStatus(bool isActive)
    {
        IsActive = isActive;
    }
}