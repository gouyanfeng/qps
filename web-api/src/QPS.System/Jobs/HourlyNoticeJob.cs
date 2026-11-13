using MediatR;
using Microsoft.Extensions.Logging;
using QPS.System.Application.Features.Messages;
using QPS.System.Contracts.DTOs.Messages;

namespace QPS.System.Jobs;

/// <summary>
/// 每小时例行通知任务：整点以广播公告形式向全员推送一条系统消息。
/// 业务逻辑走 MediatR 复用 CreateMessageCommand（无收件人 = 广播分支），
/// 与后台手工新建通知同一链路，落库后自动触发 SignalR 实时推送。
/// Hangfire 后台无 HTTP 上下文，SenderId 为空 = 系统消息（发布人显示 "-"）。
/// 注册入口在 Host 的 HangfireBootstrapper（cron: 0 * * * *）。
/// </summary>
public sealed class HourlyNoticeJob(IMediator mediator, ILogger<HourlyNoticeJob> logger)
{
    public async Task Run()
    {
        var now = DateTime.Now;
        var title = "系统例行通知";
        var content = $"每小时例行推送：当前时间 {now:yyyy-MM-dd HH:mm}，系统运行正常。";

        await mediator.Send(new CreateMessageCommand
        {
            Request = new MessageCreateRequest
            {
                Title = title,
                Content = content,
                IsPublished = true
            }
        });

        logger.LogInformation("[Hangfire] 每小时通知已发布：{Time}", now);
    }
}
