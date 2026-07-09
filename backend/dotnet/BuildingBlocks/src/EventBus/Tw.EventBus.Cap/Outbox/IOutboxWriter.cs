using Tw.EventBus.Abstractions;
using Tw.Uow;

namespace Tw.EventBus.Cap.Outbox;

public interface IOutboxWriter
{
    Task WriteAsync(IUnitOfWork unitOfWork, IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
