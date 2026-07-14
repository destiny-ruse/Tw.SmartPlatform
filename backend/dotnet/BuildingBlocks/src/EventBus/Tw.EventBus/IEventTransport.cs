namespace Tw.EventBus;

/// <summary>
/// 定义集成事件发布器与具体消息提供程序之间的传输边界
/// </summary>
public interface IEventTransport
{
    /// <summary>
    /// 把集成事件写入具体传输实现
    /// </summary>
    /// <param name="integrationEvent">需要传输的集成事件</param>
    /// <param name="cancellationToken">中止传输过程的调用方取消令牌</param>
    /// <returns>事件传输完成任务</returns>
    /// <exception cref="OperationCanceledException">调用方在传输期间请求取消</exception>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
