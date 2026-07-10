namespace Tw.EventBus.Abstractions;

/// <summary>
/// 定义事件处理器的能力边界
/// </summary>
/// <typeparam name="TEvent">响应数据的运行时类型</typeparam>
public interface IEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    /// <summary>
    /// 说明处理Async在当前类型中的职责
    /// </summary>
    /// <param name="integrationEvent">用于提供ntegrationEvent</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
