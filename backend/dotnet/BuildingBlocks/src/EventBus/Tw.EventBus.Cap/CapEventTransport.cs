using Tw.Data.Uow;
using Tw.EventBus.Abstractions;
using Tw.EventBus.Cap.Outbox;

namespace Tw.EventBus.Cap;

/// <summary>
/// 将集成事件写入当前工作单元覆盖的 CAP Outbox 事务边界
/// </summary>
/// <param name="unitOfWorkCoordinator">提供当前活动工作单元的协调器</param>
/// <param name="outboxWriter">在当前工作单元中持久化集成事件的写入器</param>
public sealed class CapEventTransport(
    IUnitOfWorkCoordinator unitOfWorkCoordinator,
    IOutboxWriter outboxWriter) : IEventTransport
{
    /// <summary>
    /// 在当前活动工作单元中写入集成事件
    /// </summary>
    /// <param name="integrationEvent">需要持久化到 Outbox 的集成事件</param>
    /// <param name="cancellationToken">等待 Outbox 写入完成时使用的取消令牌</param>
    /// <returns>Outbox 写入完成任务</returns>
    /// <exception cref="ArgumentNullException"><paramref name="integrationEvent"/> 为 <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">当前不存在工作单元，或工作单元无法覆盖 Outbox 写入</exception>
    public Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var current = unitOfWorkCoordinator.Current;
        if (current is null)
        {
            throw new InvalidOperationException("CAP Outbox 写入要求当前存在活动工作单元事务。");
        }

        if (current is not IOutboxTransactionBoundary { CanWriteOutbox: true, IsCompleted: false })
        {
            throw new InvalidOperationException("当前工作单元无法同时覆盖业务写入与 CAP Outbox 写入。");
        }

        return outboxWriter.WriteAsync(current, integrationEvent, cancellationToken);
    }
}
