# Tw.Data

`Tw.Data` 提供存储中立的数据访问、乐观并发检查和工作单元契约。领域实体的审计、并发戳、版本戳与软删除标记由 `Tw.Domain` 提供。

## 公开能力

- `IRepository<TEntity, TKey>`
- `IConcurrencyCheckContext`
- `ConcurrencyConflictException`
- `IUnitOfWork`
- `IUnitOfWorkCoordinator`
- `UnitOfWorkOptions`
- `UnitOfWorkScope`
- `UnitOfWorkTransactionBehavior`
- `IOutboxTransactionBoundary`

工作单元类型位于 `Tw.Data.Uow` 命名空间。

## 依赖边界

本包不得依赖 SqlSugar、CAP、ASP.NET Core 或其他基础设施适配包。ORM 工作单元实现进入对应的数据适配包。

## 使用方式

```csharp
await using var unitOfWork = await unitOfWorkCoordinator.BeginAsync(
    UnitOfWorkOptions.Default,
    cancellationToken);

await unitOfWork.CommitAsync(cancellationToken);
```
