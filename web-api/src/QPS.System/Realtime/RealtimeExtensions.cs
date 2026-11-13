using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace QPS.System.Realtime;

public static class RealtimeExtensions
{
    /// <summary>注册站内消息实时推送（SignalR）所需服务</summary>
    public static IServiceCollection AddQpsSystemRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IUserIdProvider, QpsUserIdProvider>();
        services.AddScoped<ISystemMessageNotifier, SystemMessageNotifier>();
        return services;
    }

    /// <summary>映射消息推送 Hub 端点</summary>
    public static IEndpointRouteBuilder MapQpsSystemRealtime(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<SystemMessageHub>("/hubs/system-messages");
        return endpoints;
    }
}
