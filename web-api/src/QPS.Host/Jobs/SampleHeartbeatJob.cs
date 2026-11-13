namespace QPS.Host.Jobs;

/// <summary>
/// 示例心跳任务：验证 Hangfire 调度链路（注册 → 触发 → Dashboard 可见）。
/// 链路验证通过后可删除，或改造成真实业务任务。
///
/// 任务编写约定：
/// 1. 任务类与执行方法必须是 public，方法参数只用简单类型（Hangfire 按方法签名序列化）；
/// 2. 业务逻辑走 MediatR：任务方法里解析 IMediator 后 Send 命令，
///    与 Controller 一样只做调度入口，不直接操作 DbContext；
/// 3. 触发方式三选一（调用处通常在本目录 HangfireBootstrapper 或各模块 DI 中）：
///    - 周期任务  RecurringJob.AddOrUpdate&lt;XxxJob&gt;("job-id", j => j.Run(), cron 表达式);
///    - 立即执行  BackgroundJob.Enqueue&lt;XxxJob&gt;(j => j.Run());
///    - 延迟执行  BackgroundJob.Schedule&lt;XxxJob&gt;(j => j.Run(), TimeSpan.FromHours(1));
/// </summary>
public sealed class SampleHeartbeatJob(ILogger<SampleHeartbeatJob> logger)
{
    public Task Run()
    {
        logger.LogInformation("[Hangfire] 心跳任务执行于 {Time}", DateTime.Now);
        return Task.CompletedTask;
    }
}
