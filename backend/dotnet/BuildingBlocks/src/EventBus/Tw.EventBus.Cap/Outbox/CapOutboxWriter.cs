using Tw.Data.Uow;
using Tw.EventBus.Abstractions;

namespace Tw.EventBus.Cap.Outbox;

/// <summary>
/// 将集成事件交给 CAP Outbox 持久化边界
/// </summary>
public sealed class CapOutboxWriter : IOutboxWriter
{
    /// <summary>
    /// 接受当前工作单元中的集成事件写入请求
    /// </summary>
    /// <param name="unitOfWork">覆盖业务写入与 Outbox 写入的工作单元</param>
    /// <param name="integrationEvent">需要持久化到 Outbox 的集成事件</param>
    /// <param name="cancellationToken">等待 Outbox 写入完成时使用的取消令牌</param>
    /// <returns>Outbox 写入完成任务</returns>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/> 或 <paramref name="integrationEvent"/> 为 <see langword="null"/></exception>
    public Task WriteAsync(
        IUnitOfWork unitOfWork,
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return Task.CompletedTask;
    }
}
