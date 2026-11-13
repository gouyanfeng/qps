using QPS.System.Contracts.DTOs.Messages;

namespace QPS.System.Realtime;

/// <summary>
/// 站内消息实时推送通道。Application 层只依赖此抽象，不直接触达 SignalR 类型。
/// 推送仅作即时提醒，数据一致性始终以数据库为准。
/// </summary>
public interface ISystemMessageNotifier
{
    /// <summary>广播公告推送：所有在线连接（含发布者本人，作为发布成功的确认）</summary>
    Task PushBroadcastAsync(MyMessageDto message, CancellationToken cancellationToken = default);

    /// <summary>点对点私信推送：仅指定收件人的全部连接</summary>
    Task PushDirectAsync(IReadOnlyCollection<Guid> recipientIds, MyMessageDto message, CancellationToken cancellationToken = default);

    /// <summary>未读数变更推送（多端同步，如另一台设备读了消息）</summary>
    Task PushUnreadAsync(Guid userId, UnreadCountDto count, CancellationToken cancellationToken = default);

    /// <summary>消息被删除 / 下架后通知客户端从列表移除</summary>
    Task PushRemovedAsync(Guid messageId, CancellationToken cancellationToken = default);
}
