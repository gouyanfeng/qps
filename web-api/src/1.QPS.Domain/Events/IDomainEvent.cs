namespace QPS.Domain.Events;

/// <summary>
/// 领域事件标记接口
/// <para>Domain 层只定义该接口，不依赖任何基础设施 (如 MediatR)。
/// 由 Application 层负责将其转换为基础设施可识别的通知进行派发。</para>
/// </summary>
public interface IDomainEvent
{
}
