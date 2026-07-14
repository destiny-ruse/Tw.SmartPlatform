namespace Tw.EventBus;

/// <summary>
/// 将集成事件分发到已注入的 provider-neutral 传输边界
/// </summary>
/// <param name="transport">接收集成事件的传输实现</param>
public sealed class EventPublisher(IEventTransport transport) : IEventPublisher
{
    /// <summary>
    /// 把同一事件与取消令牌传递给传输边界一次
    /// </summary>
    /// <param name="integrationEvent">需要发布的集成事件</param>
    /// <param name="cancellationToken">中止发布过程的调用方取消令牌</param>
    /// <returns>传输边界返回的发布完成任务</returns>
    /// <exception cref="ArgumentNullException"><paramref name="integrationEvent"/> 为 <see langword="null"/></exception>
    /// <exception cref="OperationCanceledException">调用方或传输边界请求取消</exception>
    /// <remarks>传输错误不在此层转换、吞掉或重试</remarks>
    public Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return transport.PublishAsync(integrationEvent, cancellationToken);
    }
}
