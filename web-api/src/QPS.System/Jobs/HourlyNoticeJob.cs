using MediatR;
using Microsoft.Extensions.Logging;
using QPS.System.Application.Features.Messages;

namespace QPS.System.Jobs;

/// <summary>
/// 每小时例行通知任务：整点以广播公告形式向全员推送一条系统消息。
/// 业务逻辑走 PublishSystemNoticeCommand（实现 IAllowAnonymousRequest）：
/// Hangfire 后台没有 HTTP 上下文，若直接发 CreateMessageCommand 会被 PermissionBehaviour
/// 以「登录状态无效」拒绝，任务将反复重试失败。
/// 注册入口在 Host 的 HangfireBootstrapper（cron: 0 * * * *）。
/// </summary>
public sealed class HourlyNoticeJob(IMediator mediator, ILogger<HourlyNoticeJob> logger)
{
    public async Task Run()
    {
        var now = DateTime.Now;

        await mediator.Send(new PublishSystemNoticeCommand
        {
            Title = "系统例行通知",
            Content = $"每小时例行推送：当前时间 {now:yyyy-MM-dd HH:mm}，系统运行正常。"
        });

        logger.LogInformation("[Hangfire] 每小时通知已发布：{Time}", now);
    }
}
