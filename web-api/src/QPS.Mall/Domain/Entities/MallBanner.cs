using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

public enum MallBannerLinkType
{
    InternalProduct = 0,
    InternalCategory = 1,
    InternalPage = 2,
    ExternalUrl = 3,
    InternalCampaign = 6,
    InternalPromotion = 7
}

public class MallBanner : BaseEntity
{
    public string Position { get; private set; } = string.Empty;

    public string ImageUrl { get; private set; } = string.Empty;

    public MallBannerLinkType LinkType { get; private set; }

    public string LinkValue { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public DateTime? StartDate { get; private set; }

    public DateTime? EndDate { get; private set; }

    public bool IsActive { get; private set; }

    private MallBanner()
    {
    }

    private MallBanner(
        string position,
        string imageUrl,
        MallBannerLinkType linkType,
        string linkValue,
        string title,
        int sortOrder,
        DateTime? startDate,
        DateTime? endDate,
        bool isActive)
    {
        Position = position;
        ImageUrl = imageUrl;
        LinkType = linkType;
        LinkValue = linkValue;
        Title = title;
        SortOrder = sortOrder;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }

    public static MallBanner Create(
        string position,
        string imageUrl,
        MallBannerLinkType linkType,
        string linkValue,
        string title,
        int sortOrder,
        DateTime? startDate,
        DateTime? endDate,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(position)) throw new BusinessException(400, "展示位置不能为空");
        if (string.IsNullOrWhiteSpace(imageUrl)) throw new BusinessException(400, "Banner图片不能为空");

        return new MallBanner(position, imageUrl, linkType, linkValue, title, sortOrder, startDate, endDate, isActive);
    }

    public void Update(
        string position,
        string imageUrl,
        MallBannerLinkType linkType,
        string linkValue,
        string title,
        DateTime? startDate,
        DateTime? endDate)
    {
        Position = position;
        ImageUrl = imageUrl;
        LinkType = linkType;
        LinkValue = linkValue;
        Title = title;
        StartDate = startDate;
        EndDate = endDate;
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    public void ChangeStatus(bool isActive)
    {
        IsActive = isActive;
    }
}