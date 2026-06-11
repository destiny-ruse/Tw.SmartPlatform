# HttpContext 取消令牌 Provider 使用指南

## 能力定位

`HttpContextCancellationTokenProvider`（命名空间 `Tw.AspNetCore.Mvc.Context`）是 `Tw.AspNetCore.Mvc` 的 ASP.NET Core MVC/Web API 适配能力。它将 `ICancellationTokenProvider` 的默认令牌来源替换为 `HttpContext.RequestAborted`，用于需要读取 HTTP 请求断开信号的 MVC controller action、Web API 入口和由 MVC 集成入口注册的服务。

## DI 注册

在服务注册阶段调用：

```csharp
using Tw.AspNetCore.Mvc.Context;

builder.Services.AddHttpContextCancellationTokenProvider();
```

`AddHttpContextCancellationTokenProvider()` 会注册 `Tw.Core` 取消令牌核心能力、注册 `IHttpContextAccessor`，并将 `ICancellationTokenProvider` 替换为 `HttpContextCancellationTokenProvider`。

如果应用启用完整 MVC integration，可以改用聚合入口：

```csharp
using Tw.AspNetCore.Mvc;

builder.Services.AddMvcIntegration();
```

`AddMvcIntegration()` 内部会调用 `AddHttpContextCancellationTokenProvider()`。

## 在业务服务中读取请求取消信号

业务服务注入 `ICancellationTokenProvider` 后，可以用显式参数优先、请求取消令牌兜底的方式向下游传递取消信号：

```csharp
using Tw.Context;

public sealed class OrderApplicationService
{
    private readonly ICancellationTokenProvider _cancellationTokenProvider;
    private readonly IOrderRepository _repository;

    public OrderApplicationService(
        ICancellationTokenProvider cancellationTokenProvider,
        IOrderRepository repository)
    {
        _cancellationTokenProvider = cancellationTokenProvider;
        _repository = repository;
    }

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var effectiveToken = _cancellationTokenProvider.FallbackToProvider(cancellationToken);
        return _repository.SaveAsync(effectiveToken);
    }
}
```

## 注意事项

- `Tw.AspNetCore.Mvc.Context` 是 `Tw.AspNetCore.Mvc` 包内命名空间。
- 显式传入的取消令牌优先级高于 `HttpContext.RequestAborted`。
- 没有 `HttpContext` 且没有显式令牌时，provider 返回 `CancellationToken.None`。
- provider 不负责错误响应、状态码映射、重试或超时策略。
- gRPC 专属取消令牌适配不由本包承载。
