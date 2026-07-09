namespace Tw.EventBus.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
