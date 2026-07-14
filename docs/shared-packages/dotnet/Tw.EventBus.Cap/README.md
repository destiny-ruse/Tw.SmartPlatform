# Tw.EventBus.Cap

`Tw.EventBus.Cap` 提供 CAP 传输边界、RabbitMQ 选项、SqlSugar CAP 存储元数据、Inbox 契约、消费者上下文过滤器和清理契约。当前稳定性为 `experimental`。

## 事务规则

CAP Outbox 只允许写入当前 `Tw.Data.Uow` 工作单元覆盖的活动事务边界。本包不会在当前工作单元之外创建独立 Outbox 事务；当前工作单元不存在、已经结束或不能覆盖 Outbox 写入时，发布会被拒绝。

## 公开能力

- `CapEventTransport`
- `IOutboxWriter`
- `CapRabbitMqOptions`
- `SqlSugarCapStorageSchema`
- `IInboxMessageStore`
- `CapConsumerExecutionFilter`
- `CapMessageCleanupOptions`

## 依赖边界

本包可以依赖 CAP、RabbitMQ、`Microsoft.Extensions.DependencyInjection.Abstractions`、`Tw.EventBus`、`Tw.Data` 和 `Tw.Data.SqlSugar`，不得承载业务事件契约、业务处理器或 ASP.NET Core 中间件。`Tw.EventBus` 的公开契约不包含 CAP 类型。

## 使用方式

```csharp
services.AddCapEventBus(
    rabbit =>
    {
        rabbit.HostName = "rabbitmq";
        rabbit.UserName = "cap";
        rabbit.Password = password;
    },
    storage => storage.ConnectionName = "Default");
```

## Stable 门禁

进入 `stable` 前必须通过独立 provider spec/plan 使用真实 CAP、RabbitMQ 和 SqlSugar 依赖验证 delivery、失败传播、重试与恢复、Inbox 去重和 Outbox 原子性。当前单元与契约测试不替代真实 delivery/Inbox/Outbox 集成验证。
