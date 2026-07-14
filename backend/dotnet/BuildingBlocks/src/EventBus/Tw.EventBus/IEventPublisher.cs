namespace Tw.EventBus;

/// <summary>
/// 发布 provider-neutral 集成事件
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// 将集成事件交给已配置的传输边界
    /// </summary>
    /// <param name="integrationEvent">需要发布的集成事件</param>
    /// <param name="cancellationToken">中止发布过程的调用方取消令牌</param>
    /// <returns>事件发布完成任务</returns>
    /// <exception cref="ArgumentNullException"><paramref name="integrationEvent"/> 为 <see langword="null"/></exception>
    /// <exception cref="OperationCanceledException">调用方在发布期间请求取消</exception>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
