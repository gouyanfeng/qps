using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 商城活动：营销活动主体，可关联多个促销。
/// </summary>
public class MallActivity : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Subtitle { get; private set; } = string.Empty;

    public string CoverImage { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string DetailHtml { get; private set; } = string.Empty;

    public DateTime? StartDate { get; private set; }

    public DateTime? EndDate { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    private MallActivity()
    {
    }

    private MallActivity(
        string name,
        string subtitle,
        string coverImage,
        string description,
        string detailHtml,
        DateTime? startDate,
        DateTime? endDate,
        int sortOrder,
        bool isActive)
    {
        Name = name;
        Subtitle = subtitle;
        CoverImage = coverImage;
        Description = description;
        DetailHtml = detailHtml;
        StartDate = startDate;
        EndDate = endDate;
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public static MallActivity Create(
        string name,
        string subtitle,
        string coverImage,
        string description,
        string detailHtml,
        DateTime? startDate,
        DateTime? endDate,
        int sortOrder,
        bool isActive)
    {
        return new MallActivity(
            name,
            subtitle,
            coverImage,
            description,
            detailHtml,
            startDate,
            endDate,
            sortOrder,
            isActive);
    }

    public void Update(
        string name,
        string subtitle,
        string coverImage,
        string description,
        string detailHtml,
        DateTime? startDate,
        DateTime? endDate,
        int sortOrder)
    {
        Name = name;
        Subtitle = subtitle;
        CoverImage = coverImage;
        Description = description;
        DetailHtml = detailHtml;
        StartDate = startDate;
        EndDate = endDate;
        SortOrder = sortOrder;
    }

    public void ChangeStatus(bool isActive) => IsActive = isActive;

    public bool IsWithinTimeWindow(DateTime now)
    {
        if (StartDate.HasValue && StartDate.Value > now)
        {
            return false;
        }

        if (EndDate.HasValue && EndDate.Value < now)
        {
            return false;
        }

        return true;
    }
}