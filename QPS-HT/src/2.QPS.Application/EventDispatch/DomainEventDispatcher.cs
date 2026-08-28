using MediatR;
using QPS.Application.Interfaces;
using QPS.Domain.Events;

namespace QPS.Application.EventDispatch;

/// <summary>
/// 领域事件派发器：基于 MediatR 的实现
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;

    public DomainEventDispatcher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        return _publisher.Publish(new DomainEventNotification<TEvent>(@event), cancellationToken);
    }
}
