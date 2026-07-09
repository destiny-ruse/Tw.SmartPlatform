namespace Tw.EventBus.Abstractions;

/// <summary>定义 IEventHandler 契约</summary>
/// <typeparam name="TEvent">TEvent 类型参数</typeparam>
public interface IEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    /// <summary>执行 HandleAsync 操作</summary>
    /// <param name="integrationEvent">integrationEvent 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>HandleAsync 的执行结果</returns>
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
