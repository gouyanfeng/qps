using QPS.System.Domain.Entities;

namespace QPS.System.Contracts.DTOs.Messages;

/// <summary>管理端消息列表 / 详情</summary>
public class MessageDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public SystemMessageType MessageType { get; set; }
    public Guid? SenderId { get; set; }
    public string? SenderName { get; set; }
    public bool IsTop { get; set; }
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>用户端消息（铃铛列表 / 收件箱），已附带当前用户状态</summary>
public class MyMessageDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public SystemMessageType MessageType { get; set; }
    public string? SenderName { get; set; }
    public bool IsTop { get; set; }
    public bool IsRead { get; set; }
    public bool IsHidden { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class MessageCreateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsTop { get; set; }
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsPublished { get; set; }
    /// <summary>点对点收件人；为空即广播</summary>
    public List<Guid>? RecipientIds { get; set; }
}

public class MessageUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsTop { get; set; }
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class MessageQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Keyword { get; set; }
    public SystemMessageType? MessageType { get; set; }
    public bool? IsPublished { get; set; }
}

/// <summary>用户端列表范围：广播通知 / 私信收件箱</summary>
public enum MyMessageBox
{
    Notices,
    Inbox
}

public class UnreadCountDto
{
    public int Notices { get; set; }
    public int Inbox { get; set; }
    public int Total { get; set; }
}
