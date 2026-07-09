using Tw.EventBus.Abstractions;
using Tw.Uow;

namespace Tw.EventBus.Cap.Outbox;

/// <summary>表示 CapOutboxWriter 类型</summary>
public sealed class CapOutboxWriter : IOutboxWriter
{
    /// <summary>执行 WriteAsync 操作</summary>
    /// <param name="unitOfWork">unitOfWork 参数</param>
    /// <param name="integrationEvent">integrationEvent 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>WriteAsync 的执行结果</returns>
    public Task WriteAsync(IUnitOfWork unitOfWork, IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return Task.CompletedTask;
    }
}
