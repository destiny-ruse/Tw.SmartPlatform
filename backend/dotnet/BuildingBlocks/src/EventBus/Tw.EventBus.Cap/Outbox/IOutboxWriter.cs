using Tw.EventBus.Abstractions;
using Tw.Uow;

namespace Tw.EventBus.Cap.Outbox;

/// <summary>定义 IOutboxWriter 契约</summary>
public interface IOutboxWriter
{
    /// <summary>执行 WriteAsync 操作</summary>
    /// <param name="unitOfWork">unitOfWork 参数</param>
    /// <param name="integrationEvent">integrationEvent 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>WriteAsync 的执行结果</returns>
    Task WriteAsync(IUnitOfWork unitOfWork, IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
