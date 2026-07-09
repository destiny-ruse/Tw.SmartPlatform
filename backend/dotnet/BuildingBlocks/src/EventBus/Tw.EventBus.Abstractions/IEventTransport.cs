namespace Tw.EventBus.Abstractions;

/// <summary>定义 IEventTransport 契约</summary>
public interface IEventTransport
{
    /// <summary>执行 PublishAsync 操作</summary>
    /// <param name="integrationEvent">integrationEvent 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>PublishAsync 的执行结果</returns>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
