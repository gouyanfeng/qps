using QPS.Shared.Common;

namespace QPS.System.Domain.Entities;

/// <summary>
/// 消息类型：Broadcast 全员广播公告；Direct 点对点私信（收件人写 SystemMessageRecipient）
/// </summary>
public enum SystemMessageType
{
    Broadcast,
    Direct
}

/// <summary>
/// 站内消息主体（广播 + 点对点共用一张内容表）。
/// 广播：读时计算未读，用户侧状态懒写在 SystemMessageRecipient；
/// 私信：发送时为每个收件人写一行 Recipient。
/// </summary>
public class SystemMessage : BaseEntity
{
    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public SystemMessageType MessageType { get; private set; }

    /// <summary>发送者 / 公告发布管理员</summary>
    public Guid? SenderId { get; private set; }

    /// <summary>广播：是否置顶</summary>
    public bool IsTop { get; private set; }

    /// <summary>广播：排序权重，小的在前</summary>
    public int SortOrder { get; private set; }

    /// <summary>广播：展示开始时间（到点可见，可做定时发布）</summary>
    public DateTime? StartDate { get; private set; }

    /// <summary>广播：展示结束时间（过期自动不可见）</summary>
    public DateTime? EndDate { get; private set; }

    /// <summary>广播：是否已发布（下架置 false）；私信发出即为 true</summary>
    public bool IsPublished { get; private set; }

    public virtual ICollection<SystemMessageRecipient> Recipients { get; private set; } = new List<SystemMessageRecipient>();

    private SystemMessage()
    {
    }

    private SystemMessage(
        string title,
        string content,
        SystemMessageType messageType,
        Guid? senderId,
        bool isTop,
        int sortOrder,
        DateTime? startDate,
        DateTime? endDate,
        bool isPublished)
    {
        Title = title;
        Content = content;
        MessageType = messageType;
        SenderId = senderId;
        IsTop = isTop;
        SortOrder = sortOrder;
        StartDate = startDate;
        EndDate = endDate;
        IsPublished = isPublished;
    }

    public static SystemMessage CreateBroadcast(
        string title,
        string content,
        Guid? senderId,
        bool isTop,
        int sortOrder,
        DateTime? startDate,
        DateTime? endDate,
        bool isPublished)
    {
        return new SystemMessage(title, content, SystemMessageType.Broadcast, senderId,
            isTop, sortOrder, startDate, endDate, isPublished);
    }

    public static SystemMessage CreateDirect(
        string title,
        string content,
        Guid senderId)
    {
        return new SystemMessage(title, content, SystemMessageType.Direct,
            senderId, false, 0, null, null, true);
    }

    public void Update(string title, string content,
        bool isTop, int sortOrder, DateTime? startDate, DateTime? endDate)
    {
        Title = title;
        Content = content;
        IsTop = isTop;
        SortOrder = sortOrder;
        StartDate = startDate;
        EndDate = endDate;
    }

    public void ChangePublishStatus(bool isPublished)
    {
        IsPublished = isPublished;
    }
}
