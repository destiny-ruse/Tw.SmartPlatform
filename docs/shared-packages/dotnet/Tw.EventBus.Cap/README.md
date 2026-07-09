# Tw.EventBus.Cap

`Tw.EventBus.Cap` binds CAP-oriented publishing, RabbitMQ options, SqlSugar CAP storage metadata, Inbox dedupe, consumer context validation, and cleanup contracts.

## Transaction Rule

CAP Outbox writes are valid only inside the active `Tw.Uow` transaction. The package does not create a separate Outbox transaction outside the current unit of work. CAP consumption uses Inbox records to deduplicate delivered messages. Host CAP consumers call `ISender.Send(...)` inside the dispatch delegate after tenant, shard, and culture headers are validated.

## Public Capabilities

- `CapEventTransport`
- `IOutboxWriter`
- `CapRabbitMqOptions`
- `SqlSugarCapStorageSchema`
- `IInboxMessageStore`
- `CapConsumerExecutionFilter`
- `CapMessageCleanupOptions`

## Dependency Boundary

The package can reference CAP, RabbitMQ, `Tw.EventBus`, `Tw.Uow`, and `Tw.Data.SqlSugar`. It must not host business handlers or ASP.NET Core middleware.

## Usage

```csharp
services.AddCapEventBus(
    rabbit => { rabbit.HostName = "rabbitmq"; rabbit.UserName = "cap"; rabbit.Password = password; },
    storage => storage.ConnectionName = "Default");
```
