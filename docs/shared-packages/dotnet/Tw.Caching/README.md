# Tw.Caching

`Tw.Caching` defines tenant and shard aware cache keys and invalidation contracts.

## Usage

```csharp
var key = CacheKeyBuilder.Build("tenant-a", "orders-2026", "Order", "42", "v3");
```
