using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public enum MallNoticeType
{
    System,
    Activity,
    Maintenance
}

public class MallNotice : BaseEntity
{
    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public MallNoticeType NoticeType { get; private set; }

    public bool IsTop { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime? StartDate { get; private set; }

    public DateTime? EndDate { get; private set; }

    public bool IsActive { get; private set; }

    private MallNotice()
    {
    }

    private MallNotice(
        string title,
        string content,
        MallNoticeType noticeType,
        bool isTop,
        int sortOrder,
        DateTime? startDate,
        DateTime? endDate,
        bool isActive)
    {
        Title = title;
        Content = content;
        NoticeType = noticeType;
        IsTop = isTop;
        SortOrder = sortOrder;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }

    public static MallNotice Create(
        string title,
        string content,
        MallNoticeType noticeType,
        bool isTop,
        int sortOrder,
        DateTime? startDate,
        DateTime? endDate,
        bool isActive)
    {
        return new MallNotice(title, content, noticeType, isTop, sortOrder, startDate, endDate, isActive);
    }

    public void Update(
        string title,
        string content,
        MallNoticeType noticeType,
        int sortOrder,
        DateTime? startDate,
        DateTime? endDate)
    {
        Title = title;
        Content = content;
        NoticeType = noticeType;
        SortOrder = sortOrder;
        StartDate = startDate;
        EndDate = endDate;
    }

    public void SetTop(bool isTop)
    {
        IsTop = isTop;
    }

    public void ChangeStatus(bool isActive)
    {
        IsActive = isActive;
    }
}