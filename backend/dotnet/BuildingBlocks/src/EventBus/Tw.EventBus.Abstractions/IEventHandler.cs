namespace Tw.EventBus.Abstractions;

public interface IEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
