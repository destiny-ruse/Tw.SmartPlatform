using Tw.EventBus.Abstractions;
using Tw.EventBus.Cap.Outbox;
using Tw.Uow;

namespace Tw.EventBus.Cap;

/// <summary>
/// 封装Cap事件Transport相关的数据和行为
/// </summary>
public sealed class CapEventTransport(IUnitOfWorkManager unitOfWorkManager, IOutboxWriter outboxWriter) : IEventTransport
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

        var current = unitOfWorkManager.Current;
        if (current is null)
        {
            throw new InvalidOperationException("CAP Outbox writes require the current unit of work transaction.");
        }

        if (current is not IOutboxTransactionBoundary { CanWriteOutbox: true })
        {
            throw new InvalidOperationException("The current unit of work cannot cover business writes and CAP Outbox writes.");
        }

        return outboxWriter.WriteAsync(current, integrationEvent, cancellationToken);
    }
}
