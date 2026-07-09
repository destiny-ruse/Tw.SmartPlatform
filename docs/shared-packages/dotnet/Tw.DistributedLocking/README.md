# Tw.DistributedLocking

`Tw.DistributedLocking` defines distributed lock contracts and tenant/shard aware lock keys.

## Usage

```csharp
var key = DistributedLockKeyBuilder.Build("tenant-a", "shard-01", "Invoice", "inv-100");
```
