namespace Tw.EventBus.Abstractions;

public interface IEventTransport
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
