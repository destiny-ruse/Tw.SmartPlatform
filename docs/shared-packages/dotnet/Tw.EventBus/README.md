# Tw.EventBus

`Tw.EventBus` 是 provider-neutral 集成事件基础包，包含事件元数据、发布器、处理器和 transport 契约，以及单次委托到 transport 的默认发布器。当前稳定性为 `experimental`。

## 公开能力

- `IIntegrationEvent`：公开只读 `EventId` 元数据
- `IEventPublisher`：应用侧事件发布入口
- `IEventTransport`：消息 provider 的传输边界
- `IEventHandler<TEvent>`：强类型集成事件处理契约
- `EventPublisher`：把同一事件和取消令牌传递给 transport 一次

## 依赖边界

本包不依赖 CAP、RabbitMQ、SqlSugar 或 ASP.NET Core，公开契约也不暴露这些第三方类型。宿主在组合根注册 `IEventTransport`，并将 `IEventPublisher` 注册为 `EventPublisher`；本包不自动选择 transport。

## 使用方式

```csharp
await publisher.PublishAsync(
    new OrderCreated("event-1"),
    cancellationToken);
```

`EventPublisher` 不吞掉取消或 transport 异常，也不在 provider-neutral 层隐式重试。投递、重试、Inbox 和 Outbox 语义由具体 provider 与宿主策略负责。
