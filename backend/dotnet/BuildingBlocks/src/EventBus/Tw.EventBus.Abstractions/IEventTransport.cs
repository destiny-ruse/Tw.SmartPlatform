namespace Tw.EventBus.Abstractions;

/// <summary>
/// 定义事件Transport的能力边界
/// </summary>
public interface IEventTransport
{
    /// <summary>
    /// 发布集成事件到测试事件总线
    /// </summary>
    /// <param name="integrationEvent">用于提供ntegrationEvent</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
