# Tw.Data.SqlSugar

`Tw.Data.SqlSugar` adapts the shared `Tw.Uow` contract to SqlSugar client creation and exposes a transaction boundary for Outbox coordination.

## Public Capabilities

- `IConnectionConfigResolver`
- `ISqlSugarClientFactory`
- `SqlSugarUnitOfWorkManager`
- `SqlSugarUnitOfWork`

## Dependency Boundary

This package can reference `SqlSugarCore`, `Tw.Data`, `Tw.Uow`, and `Tw.Core`. It must not reference CAP, ASP.NET Core, Quartz, or Gateway packages.

## Usage

```csharp
await using var uow = await unitOfWorkManager.BeginAsync(UnitOfWorkOptions.Default, cancellationToken);
await uow.CommitAsync(cancellationToken);
```
