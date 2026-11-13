using QPS.Shared.Common;

namespace QPS.System.Domain.Entities;

/// <summary>
/// 用户与消息的状态行（懒写）：
/// - 私信：发送时即插行（IsRead=false）；
/// - 广播：用户读/隐藏时才插行；未读广播 = 已发布广播数 - 该用户已读广播行数。
/// IsDeleted 仅表示"从我的列表隐藏"，不影响消息本体与其他用户。
/// </summary>
public class SystemMessageRecipient : BaseEntity
{
    public Guid MessageId { get; private set; }

    public Guid UserId { get; private set; }

    public bool IsRead { get; private set; }

    public DateTime? ReadAt { get; private set; }

    public virtual SystemMessage? Message { get; private set; }

    private SystemMessageRecipient()
    {
    }

    public SystemMessageRecipient(Guid messageId, Guid userId, bool isRead = false)
    {
        MessageId = messageId;
        UserId = userId;
        IsRead = isRead;
        ReadAt = isRead ? DateTime.Now : null;
    }

    public void MarkRead()
    {
        IsRead = true;
        ReadAt = DateTime.Now;
    }

    public void HideFromInbox()
    {
        IsDeleted = true;
    }
}
