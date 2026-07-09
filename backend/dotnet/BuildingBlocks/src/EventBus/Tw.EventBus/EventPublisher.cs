using Tw.EventBus.Abstractions;

namespace Tw.EventBus;

/// <summary>表示 EventPublisher 类型</summary>
public sealed class EventPublisher(IEventTransport transport) : IEventPublisher
{
    /// <summary>执行 PublishAsync 操作</summary>
    /// <param name="integrationEvent">integrationEvent 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>PublishAsync 的执行结果</returns>
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return transport.PublishAsync(integrationEvent, cancellationToken);
    }
}
