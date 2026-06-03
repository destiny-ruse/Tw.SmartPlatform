# HttpContext 取消令牌 Provider 使用指南

## 能力定位

`HttpContextCancellationTokenProvider`（命名空间 `Tw.AspNetCore.Context`）是 `Tw.AspNetCore` 项目内的 ASP.NET Core 适配能力，不是独立项目。它将 `ICancellationTokenProvider` 的默认令牌来源替换为 `HttpContext.RequestAborted`，用于 HTTP API、运行在 ASP.NET Core 宿主内的 gRPC 服务，以及需要读取请求断开信号的 Web 入口。

## DI 注册

ASP.NET Core 宿主注册：

```csharp
services.AddTwAspNetCore();
```

`AddTwAspNetCore` 会先注册 `Tw.Core` 核心能力，再注册 `IHttpContextAccessor`，并将 `ICancellationTokenProvider` 替换为 `HttpContextCancellationTokenProvider`。

## HTTP API

业务服务注入 `ICancellationTokenProvider` 后，默认读取当前 HTTP 请求的 `RequestAborted`：

```csharp
public Task SaveAsync(CancellationToken cancellationToken = default)
{
    var effectiveToken = cancellationTokenProvider.FallbackToProvider(cancellationToken);
    return repository.SaveAsync(effectiveToken);
}
```

## gRPC

gRPC 服务运行在 ASP.NET Core 宿主中时，默认 provider 可读取 HTTP request aborted。服务方法显式取得 `ServerCallContext.CancellationToken` 时，用 `Use(token)` 覆盖当前执行上下文：

```csharp
using (cancellationTokenProvider.Use(context.CancellationToken))
{
    return await applicationService.HandleAsync();
}
```

## 注意事项

- `Tw.AspNetCore.Context` 是 `Tw.AspNetCore` 项目内命名空间，不是独立共享包。
- 覆盖令牌优先级高于 `HttpContext.RequestAborted`。
- 没有 `HttpContext` 且没有覆盖令牌时，provider 返回 `CancellationToken.None`。
- provider 不负责错误响应、状态码映射、重试或超时策略。
