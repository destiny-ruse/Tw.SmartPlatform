using Tw.Data.Uow;
using Tw.EventBus.Abstractions;

namespace Tw.EventBus.Cap.Outbox;

/// <summary>
/// 在工作单元覆盖的事务边界中持久化 CAP Outbox 事件
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    /// 将集成事件写入指定工作单元覆盖的 Outbox
    /// </summary>
    /// <param name="unitOfWork">覆盖业务写入与 Outbox 写入的工作单元</param>
    /// <param name="integrationEvent">需要持久化到 Outbox 的集成事件</param>
    /// <param name="cancellationToken">等待 Outbox 写入完成时使用的取消令牌</param>
    /// <returns>Outbox 写入完成任务</returns>
    Task WriteAsync(
        IUnitOfWork unitOfWork,
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default);
}
