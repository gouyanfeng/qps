using QPS.Application.Interfaces;
using QPS.Domain.Events;

namespace QPS.UnitTests.Common;

/// <summary>
/// 测试用领域事件派发器桩实现：什么都不做，仅满足依赖注入
/// </summary>
internal sealed class TestDomainEventDispatcher : IDomainEventDispatcher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        return Task.CompletedTask;
    }
}
