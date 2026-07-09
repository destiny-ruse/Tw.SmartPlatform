# Tw.EventBus

`Tw.EventBus` provides integration event publishing contracts and a transport-delegating publisher implementation.

## Public Capabilities

- `IIntegrationEvent`
- `IEventPublisher`
- `IEventTransport`
- `IEventHandler<TEvent>`
- `EventPublisher`

## Dependency Boundary

The base event bus package does not reference CAP, RabbitMQ, SqlSugar, or ASP.NET Core.

## Usage

```csharp
await publisher.PublishAsync(new OrderCreated("event-1"), cancellationToken);
```
