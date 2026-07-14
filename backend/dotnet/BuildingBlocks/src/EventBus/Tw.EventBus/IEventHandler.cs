namespace Tw.EventBus;

/// <summary>
/// 处理指定类型的集成事件
/// </summary>
/// <typeparam name="TEvent">处理器接受的集成事件类型</typeparam>
public interface IEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    /// <summary>
    /// 处理单个集成事件
    /// </summary>
    /// <param name="integrationEvent">需要处理的集成事件</param>
    /// <param name="cancellationToken">中止事件处理的调用方取消令牌</param>
    /// <returns>事件处理完成任务</returns>
    /// <exception cref="OperationCanceledException">调用方在处理期间请求取消</exception>
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
