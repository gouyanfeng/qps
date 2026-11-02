using MediatR;
using QPS.Shared.Contracts;

namespace QPS.Shared.Pipeline;

/// <summary>
/// 领域事件通知适配器
/// <para>将 Domain 层的 IDomainEvent 包装为 MediatR 可识别的 INotification，
/// 便于通过 MediatR 的 IPublisher 进行派发与 Handler 路由。</para>
/// </summary>
public sealed class DomainEventNotification<TEvent> : INotification
    where TEvent : IDomainEvent
{
    public DomainEventNotification(TEvent @event)
    {
        DomainEvent = @event;
    }

    public TEvent DomainEvent { get; }
}
