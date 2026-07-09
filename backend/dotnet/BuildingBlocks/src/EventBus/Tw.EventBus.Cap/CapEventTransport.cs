using Tw.EventBus.Abstractions;
using Tw.EventBus.Cap.Outbox;
using Tw.Uow;

namespace Tw.EventBus.Cap;

public sealed class CapEventTransport(IUnitOfWorkManager unitOfWorkManager, IOutboxWriter outboxWriter) : IEventTransport
{
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
