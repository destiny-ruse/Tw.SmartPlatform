# Tw.EventBus.Cap

`Tw.EventBus.Cap` 提供 CAP 发布、RabbitMQ 选项、SqlSugar CAP 存储元数据、Inbox 去重、消费者上下文校验和清理契约。

## 事务规则

CAP Outbox 只允许写入当前 `Tw.Data.Uow` 工作单元覆盖的事务边界。本包不会在当前工作单元之外创建独立 Outbox 事务；当前工作单元不存在或不能覆盖 Outbox 写入时，发布会被拒绝。CAP 消费通过 Inbox 记录去重，宿主消费者在租户、分片和区域性请求头校验后，由分发委托调用 `ISender.Send(...)`。

## 公开能力

- `CapEventTransport`
- `IOutboxWriter`
- `CapRabbitMqOptions`
- `SqlSugarCapStorageSchema`
- `IInboxMessageStore`
- `CapConsumerExecutionFilter`
- `CapMessageCleanupOptions`

## 依赖边界

本包可以依赖 CAP、RabbitMQ、`Tw.EventBus`、`Tw.Data` 和 `Tw.Data.SqlSugar`，不得承载业务处理器或 ASP.NET Core 中间件。

## 使用方式

```csharp
services.AddCapEventBus(
    rabbit => { rabbit.HostName = "rabbitmq"; rabbit.UserName = "cap"; rabbit.Password = password; },
    storage => storage.ConnectionName = "Default");
```
