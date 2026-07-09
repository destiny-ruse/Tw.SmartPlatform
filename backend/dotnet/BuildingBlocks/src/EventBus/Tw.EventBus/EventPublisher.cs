using Tw.EventBus.Abstractions;

namespace Tw.EventBus;

public sealed class EventPublisher(IEventTransport transport) : IEventPublisher
{
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return transport.PublishAsync(integrationEvent, cancellationToken);
    }
}
