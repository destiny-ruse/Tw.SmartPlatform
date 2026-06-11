# 启用 MVC action 拦截

本指南面向使用 `Tw.AspNetCore.Mvc` 的 .NET 开发者，目标是在 MVC/Web API 应用中启用 MVC integration，并将 controller action 接入统一 AOP 拦截 pipeline。

## 注册 MVC integration

在组合根的服务注册阶段调用 `AddMvcIntegration()`：

```csharp
using Tw.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMvcIntegration();

var app = builder.Build();
app.MapControllers();
app.Run();
```

`AddMvcIntegration()` 会注册以下 MVC 专属能力：

- `HttpContextCancellationTokenProvider`，让 `ICancellationTokenProvider` 默认读取 `HttpContext.RequestAborted`。
- 默认 `IInterceptorSelector` 与 `IInterceptorPipeline`。
- MVC action filter `TwActionInterceptionFilter`。

## 让 action 进入统一拦截 pipeline

`TwActionInterceptionFilter` 是 MVC filter adapter。MVC 执行 controller action 前，filter 会解析当前 action 的 `ControllerActionDescriptor.MethodInfo`，通过 `IInterceptorSelector` 选择应用到该 action 的拦截器。

当当前 action 没有匹配拦截器时，filter 直接继续 MVC action 管线。当存在匹配拦截器时，filter 创建 `MvcInvocationContext`，再调用统一的 `IInterceptorPipeline` 执行拦截链。拦截器因此可以复用 `Tw.DynamicProxy.Abstractions.IInterceptor` 与 `IInvocationContext` 契约。

拦截器仍按统一 AOP 模型实现：

```csharp
using Tw.DynamicProxy.Abstractions;

public sealed class AuditInterceptor : InterceptorBase
{
    public override async ValueTask InterceptAsync(IInvocationContext context)
    {
        await context.ProceedAsync();
    }
}
```

## 修改 action 参数

MVC action 参数会映射到 `MvcInvocationContext.Arguments`。拦截器可以在调用 `ProceedAsync()` 前修改 `Arguments` 中的值。

```csharp
public sealed class NormalizeNameInterceptor : InterceptorBase
{
    public override async ValueTask InterceptAsync(IInvocationContext context)
    {
        context.Arguments[0] = context.Arguments[0]?.ToString()?.Trim();

        await context.ProceedAsync();
    }
}
```

`MvcInvocationContext.ProceedAsync()` 会在继续执行 action 前，将 `IInvocationContext.Arguments` 回写到 MVC `ActionArguments`。因此，拦截器在 `ProceedAsync()` 前完成的参数改写会影响随后执行的 controller action。

## 替换 MVC result

`MvcInvocationContext.ReturnValue` 可以保存任意对象，但写回 MVC result 的边界只支持 `IActionResult`。当拦截器设置的 `ReturnValue` 是 `IActionResult` 时，MVC filter adapter 会把该值写入 MVC 的 `Result`。

短路 action 时，设置 `IActionResult`，并且不要调用 `ProceedAsync()`：

```csharp
using Microsoft.AspNetCore.Mvc;
using Tw.DynamicProxy.Abstractions;

public sealed class RejectInterceptor : InterceptorBase
{
    public override ValueTask InterceptAsync(IInvocationContext context)
    {
        context.ReturnValue = new ForbidResult();
        return ValueTask.CompletedTask;
    }
}
```

替换 action 执行后的结果时，先调用 `ProceedAsync()`，再设置新的 `IActionResult`：

```csharp
using Microsoft.AspNetCore.Mvc;

public sealed class WrapResultInterceptor : InterceptorBase
{
    public override async ValueTask InterceptAsync(IInvocationContext context)
    {
        await context.ProceedAsync();

        context.ReturnValue = new OkObjectResult(new { data = context.ReturnValue });
    }
}
```

如果拦截器把 `ReturnValue` 设置为非 `IActionResult` 对象，该值只保存在 `IInvocationContext.ReturnValue` 中，不会写回 MVC action result。需要短路或替换 MVC 响应时，必须使用 `IActionResult`。

## 边界

`AddMvcIntegration()` 当前只注册 MVC action filter，适用于 controller action 与 Web API action。

以下入口不由本能力承载：

- Middleware。
- Minimal API。
- gRPC。
- Razor Page handler。当前未实现 Razor Page handler filter adapter。

跨协议宿主启动入口仍使用 [`Tw.AspNetCore`](../Tw.AspNetCore/README.md) 的 `UseTwHostStartup()`；MVC action AOP adapter 由 `Tw.AspNetCore.Mvc` 单独注册。
