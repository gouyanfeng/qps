using Hangfire;
using Hangfire.Common;
using QPS.System.Jobs;

namespace QPS.Host.Jobs;

/// <summary>
/// Hangfire 启动引导：宿主启动时登记周期任务。
/// 放在 IHostedService 而不是 Program.cs 里直接调用，
/// 是为了确保执行时 Hangfire 存储已完成初始化（IRecurringJobManager 由 AddHangfire 注册）。
/// </summary>
public sealed class HangfireBootstrapper(IRecurringJobManager recurringJobs) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 心跳示例任务：每 5 分钟一次，验证调度链路（Dashboard 可见执行记录）。
        // 链路验证通过后可删除本行，或改造成真实业务任务。
        recurringJobs.AddOrUpdate(
            "sample-heartbeat",
            Job.FromExpression<SampleHeartbeatJob>(job => job.Run()),
            "*/5 * * * *",
            new RecurringJobOptions());

        // 每小时例行通知：整点向全员广播一条系统消息（QPS.System 模块任务）。
        recurringJobs.AddOrUpdate(
            "hourly-notice",
            Job.FromExpression<HourlyNoticeJob>(job => job.Run()),
            "0 * * * *",
            new RecurringJobOptions());

        // 新的周期任务在此登记；模块归属明确的任务也可在各模块 DependencyInjection 的
        // IHostedService 中登记，保持模块可卸载（删模块即删任务）。

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
