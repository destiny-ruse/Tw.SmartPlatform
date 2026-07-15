# Tw.Data.SqlSugar

`Tw.Data.SqlSugar` 将 `Tw.Data.Uow` 工作单元契约适配到 SqlSugar 客户端创建，并公开 CAP Outbox 协调所需的事务边界。

## 公开能力

- `IConnectionConfigResolver`
- `ISqlSugarClientFactory`
- `SqlSugarUnitOfWorkCoordinator`
- `SqlSugarUnitOfWork`

## 依赖边界

本包可以依赖 `SqlSugarCore`、`Tw.Data` 和 `Tw.Core`，不得依赖 CAP、ASP.NET Core、Quartz 或 Gateway 包。

## 使用方式

```csharp
await using var unitOfWork = await unitOfWorkCoordinator.BeginAsync(
    UnitOfWorkOptions.Default,
    cancellationToken);

await unitOfWork.CommitAsync(cancellationToken);
```

调用方负责提交或回滚，并在作用域结束时异步释放工作单元。`Required` 作用域会复用当前活动工作单元。

## 稳定性

当前状态为 `experimental`。转为 `stable` 前，必须以真实 SqlSugar provider 验证连接解析、嵌套工作单元复用、提交、回滚、异常释放和并发作用域隔离，并覆盖 CAP Outbox 使用的事务边界。
