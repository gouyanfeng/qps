using Microsoft.AspNetCore.SignalR;
using QPS.System.Contracts.DTOs.Messages;

namespace QPS.System.Realtime;

/// <summary>
/// 基于 SignalR 的推送实现。单实例部署直接广播；将来 api 多副本时需在 Host 增加 Redis backplane，
/// 本类代码无需改动。
/// </summary>
public class SystemMessageNotifier : ISystemMessageNotifier
{
    /// <summary>客户端事件名，与 admin/src/stores/modules/notice.ts 保持一致</summary>
    public const string EventReceived = "message:received";
    public const string EventRemoved = "message:removed";
    public const string EventUnread = "message:unread";

    private readonly IHubContext<SystemMessageHub> _hubContext;

    public SystemMessageNotifier(IHubContext<SystemMessageHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PushBroadcastAsync(MyMessageDto message, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.All.SendAsync(EventReceived, message, cancellationToken);
    }

    public Task PushDirectAsync(IReadOnlyCollection<Guid> recipientIds, MyMessageDto message, CancellationToken cancellationToken = default)
    {
        if (recipientIds.Count == 0)
        {
            return Task.CompletedTask;
        }

        var userIds = recipientIds.Select(id => id.ToString()).ToList();
        return _hubContext.Clients.Users(userIds).SendAsync(EventReceived, message, cancellationToken);
    }

    public Task PushUnreadAsync(Guid userId, UnreadCountDto count, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.User(userId.ToString()).SendAsync(EventUnread, count, cancellationToken);
    }

    public Task PushRemovedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.All.SendAsync(EventRemoved, new { id = messageId }, cancellationToken);
    }
}
