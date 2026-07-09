using Tw.EventBus.Abstractions;
using Tw.Uow;

namespace Tw.EventBus.Cap.Outbox;

public sealed class CapOutboxWriter : IOutboxWriter
{
    public Task WriteAsync(IUnitOfWork unitOfWork, IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return Task.CompletedTask;
    }
}
