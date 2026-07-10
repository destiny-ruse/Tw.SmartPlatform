using Tw.EventBus.Abstractions;

namespace Tw.EventBus;

/// <summary>
/// 封装事件Publisher相关的数据和行为
/// </summary>
public sealed class EventPublisher(IEventTransport transport) : IEventPublisher
{
    /// <summary>
    /// 发布集成事件到测试事件总线
    /// </summary>
    /// <param name="integrationEvent">用于提供ntegrationEvent</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return transport.PublishAsync(integrationEvent, cancellationToken);
    }
}
