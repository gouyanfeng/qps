using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace QPS.System.Realtime;

/// <summary>
/// 站内消息实时推送 Hub（admin 端）。
/// 浏览器 WebSocket 无法自定义请求头，令牌由 JWT OnMessageReceived 从 access_token 查询参数读取。
/// </summary>
[Authorize]
public class SystemMessageHub : Hub
{
    /// <summary>按用户分组的组名前缀，便于向同一用户的多端连接同时推送</summary>
    public static string UserGroup(Guid userId) => $"sysmsg-user-{userId:N}";

    public override async Task OnConnectedAsync()
    {
        // Context.UserIdentifier 由 QpsUserIdProvider 解析（userId claim）
        if (Guid.TryParse(Context.UserIdentifier, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Guid.TryParse(Context.UserIdentifier, out var userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
