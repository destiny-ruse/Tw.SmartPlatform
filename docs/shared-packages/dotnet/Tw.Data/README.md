# Tw.Data

`Tw.Data` contains storage-neutral data contracts: audited entities, soft delete, optimistic concurrency, and repository abstractions.

## Public Capabilities

- `IAuditedEntity`
- `ISoftDelete`
- `IHasConcurrencyStamp`
- `IHasVersionStamp`
- `ConcurrencyConflictException`
- `IRepository<TEntity, TKey>`

## Dependency Boundary

This package does not reference SqlSugar, CAP, ASP.NET Core, or any infrastructure adapter package.

## Usage

```csharp
public sealed class Order : IHasConcurrencyStamp
{
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");
}
```
