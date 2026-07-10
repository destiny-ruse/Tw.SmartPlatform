using Tw.EventBus.Abstractions;
using Tw.Uow;

namespace Tw.EventBus.Cap.Outbox;

/// <summary>
/// 封装CapOutboxWriter相关的数据和行为
/// </summary>
public sealed class CapOutboxWriter : IOutboxWriter
{
    /// <summary>
    /// 写入待发送或待持久化的测试消息
    /// </summary>
    /// <param name="unitOfWork">用于提供unitOfWork</param>
    /// <param name="integrationEvent">用于提供ntegrationEvent</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    public Task WriteAsync(IUnitOfWork unitOfWork, IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return Task.CompletedTask;
    }
}
