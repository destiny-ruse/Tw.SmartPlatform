# 使用异步释放工具

本指南面向需要把异步清理逻辑包装为 `IAsyncDisposable` 的 .NET 开发者，说明如何使用 `Tw.Async` 中的释放工具。

## 将异步清理委托包装为资源

当某个作用域需要在退出时调用已有的异步清理委托时，使用 `AsyncDisposeFunc`：

```csharp
using Tw.Async;

Func<ValueTask> releaseLeaseAsync = lease.DisposeAsync;

await using var leaseLifetime = new AsyncDisposeFunc(releaseLeaseAsync);
```

`AsyncDisposeFunc` 支持 `Func<Task>` 和 `Func<ValueTask>`。无论调用多少次 `DisposeAsync()`，已配置的委托最多执行一次；委托产生的异常会原样传递给调用方。

## 为可选资源提供空实现

当调用方始终需要一个可异步释放对象，但某个分支没有实际资源时，使用共享的 `NullAsyncDisposable.Instance`：

```csharp
using Tw.Async;

IAsyncDisposable lifetime = isFeatureEnabled
    ? new AsyncDisposeFunc(releaseLeaseAsync)
    : NullAsyncDisposable.Instance;

await using (lifetime)
{
    await ExecuteAsync();
}
```

`NullAsyncDisposable.Instance` 可以安全地重复使用，释放不会产生副作用。

## 边界

`Tw.Async` 只负责资源释放生命周期，不提供环境式取消令牌、请求上下文或 HTTP 适配。
