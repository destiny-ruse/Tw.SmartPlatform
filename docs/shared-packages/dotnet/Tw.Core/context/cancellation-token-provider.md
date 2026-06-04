# 取消令牌 Provider 使用指南

## 能力定位

`ICancellationTokenProvider`（命名空间 `Tw.Context`）为 HTTP API、gRPC、DotNetCore.CAP 消费、HostedService、Worker、后台任务和定时任务等入口提供统一的执行上下文取消令牌。`Tw.Core` 提供框架无关的核心实现，`Tw.AspNetCore` 提供基于 `HttpContext.RequestAborted` 的适配。

## DI 注册

非 Web 宿主注册核心能力：

```csharp
services.AddCancellationTokenProvider();
```

> `AddCancellationTokenProvider` 位于命名空间 `Tw.Context`。

ASP.NET Core 宿主注册 Web 适配，自动将 provider 替换为 HTTP provider：

```csharp
services.AddHttpContextCancellationTokenProvider();
```

## HTTP API

注册 `AddHttpContextCancellationTokenProvider` 后，业务服务注入 `ICancellationTokenProvider` 即可读取当前请求的 `RequestAborted`。业务方法存在显式 `CancellationToken` 参数时，使用 `FallbackToProvider` 实现显式令牌优先：

```csharp
public Task HandleAsync(CancellationToken cancellationToken = default)
{
    var effectiveToken = cancellationTokenProvider.FallbackToProvider(cancellationToken);
    return repository.SaveAsync(effectiveToken);
}
```

## gRPC

在方法入口用 `ServerCallContext.CancellationToken` 建立作用域：

```csharp
using (cancellationTokenProvider.Use(context.CancellationToken))
{
    return await applicationService.HandleAsync();
}
```

## DotNetCore.CAP 消费

CAP consumer 在入口方法中从可用上下文取得取消令牌，再建立作用域：

```csharp
using (cancellationTokenProvider.Use(cancellationToken))
{
    await applicationService.HandleAsync();
}
```

## HostedService / Worker / 后台任务

`BackgroundService.ExecuteAsync` 用 `stoppingToken` 建立作用域：

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using (cancellationTokenProvider.Use(stoppingToken))
    {
        await workerLoop.RunAsync();
    }
}
```

调度框架提供令牌时，入口同样使用 `Use(token)`；没有提供令牌时，provider 返回 `CancellationToken.None`。

## 业务服务推荐写法

业务方法保留显式 `CancellationToken` 参数，并在内部用 `FallbackToProvider` 取得有效令牌，使显式调用方和入口上下文都能生效。

## 注意事项

- `OperationCanceledException` 表示正常取消信号，不在业务层吞掉后返回成功。
- provider 不生成错误响应、不改变状态码、不主动取消令牌，只传播入口提供的取消信号。
- 作用域基于 `AsyncLocal`，在同一异步调用链内传播；不跨独立异步执行链传播。
- 嵌套 `Use(token)` 释放内层后恢复外层令牌。
