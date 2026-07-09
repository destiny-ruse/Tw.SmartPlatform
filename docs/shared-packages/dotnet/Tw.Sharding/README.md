# Tw.Sharding

`Tw.Sharding` stores the current shard descriptor in the async call flow.

## Public Capabilities

- `ShardDescriptor`
- `IShardContext`
- `ShardContext`

## Dependency Boundary

The package is storage-neutral and does not reference SqlSugar, CAP, ASP.NET Core, or Gateway packages.

## Usage

```csharp
using var scope = shardContext.Change(new ShardDescriptor("month", "orders-2026"));
```
