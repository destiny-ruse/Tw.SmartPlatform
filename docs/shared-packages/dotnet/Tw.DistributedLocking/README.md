# Tw.DistributedLocking

`Tw.DistributedLocking` 是 provider-neutral 分布式锁能力包，包含锁资源键、锁获取契约和租户/分片感知的稳定键构造器。当前稳定性为 `experimental`。

## 公开能力

- `DistributedLockKey`：不可为空白的锁资源标识，按 `Value` 使用值相等语义
- `DistributedLockKeyBuilder`：按租户、分片、资源类型和资源标识生成 `lock:` 键
- `IDistributedLock`：接收超时与 `CancellationToken`，返回由调用方负责异步释放的锁句柄

## 依赖边界

本包不依赖 Redis、`StackExchange.Redis`、CAP 或数据库 provider，公开契约也不暴露这些第三方类型。宿主在组合根注册选定 provider 对 `IDistributedLock` 的实现；本包不提供默认 DI 注册入口。

## 使用方式

```csharp
var key = DistributedLockKeyBuilder.Build(
    "tenant-a",
    "shard-01",
    "Invoice",
    "inv-100");

await using var handle = await distributedLock.TryAcquireAsync(
    key,
    TimeSpan.FromSeconds(3),
    cancellationToken);

if (handle is null)
{
    return;
}

// 仅在句柄所有权范围内执行受保护操作
```

`TryAcquireAsync` 返回 `null` 表示在限定时间内未获取锁。非空句柄的所有权属于调用方，调用方必须使用 `await using` 或显式 `DisposeAsync()` 释放。
