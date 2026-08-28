using QPS.Domain.Events;

namespace QPS.Application.Interfaces;

/// <summary>
/// 领域事件派发器接口
/// <para>Domain 层只产生 IDomainEvent，由 Application 层负责具体派发实现，
/// 避免 Domain 层直接依赖 MediatR 等基础设施库。</para>
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// 派发一个领域事件
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;
}
