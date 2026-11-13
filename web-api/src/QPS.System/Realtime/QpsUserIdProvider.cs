using Microsoft.AspNetCore.SignalR;

namespace QPS.System.Realtime;

/// <summary>
/// SignalR 用户标识解析：默认实现取 NameIdentifier，而本项目 JWT 用的是自定义 "userId" claim，
/// 借此让 Clients.User(userId) 能精准命中目标用户的全部连接。
/// </summary>
public class QpsUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("userId")?.Value;
    }
}
