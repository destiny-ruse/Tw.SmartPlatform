# 启用 MVC action 与 Razor Page handler 拦截

本指南面向使用 `Tw.AspNetCore.Mvc` 的 .NET 开发者，目标是在 MVC/Web API/Razor Pages 应用中启用 MVC integration，并将 controller action 与 Razor Page handler 接入统一 AOP 拦截 pipeline。

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
- Razor Page handler filter `TwPageInterceptionFilter`。

## 让 action 进入统一拦截 pipeline

`TwActionInterceptionFilter` 是 MVC filter adapter。MVC 执行 controller action 前，filter 会解析当前 action 的 `ControllerActionDescriptor.MethodInfo`，通过 `IInterceptorSelector` 选择应用到该 action 的拦截器。

当当前 action 没有匹配拦截器时，filter 直接继续 MVC action 管线。当存在匹配拦截器时，filter 创建 `MvcInvocationContext`，再调用统一的 `IInterceptorPipeline` 执行拦截链。拦截器因此可以复用 `Tw.Castle.Core.Abstractions.IInterceptor` 与 `IInvocationContext` 契约。

拦截器仍按统一 AOP 模型实现：

```csharp
using Tw.Castle.Core.Abstractions;

public sealed class AuditInterceptor : InterceptorBase
{
    public override async ValueTask InterceptAsync(IInvocationContext context)
    {
        await context.ProceedAsync();
    }
}
```

## 注册拦截器

`TwActionInterceptionFilter` 会从 DI 解析 `[Intercept]` 选中的拦截器类型。拦截器必须先注册到服务容器：

```csharp
using Tw.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMvcIntegration();
builder.Services.AddScoped<AuditInterceptor>();
```

项目已经使用统一自动注册时，也可以沿用项目现有注册方式；关键要求是拦截器类型能从当前请求的 `IServiceProvider` 解析。

## 标记 controller 或 action

默认 selector 是 `AttributeInterceptorSelector`。controller 或 action 必须使用 `[Intercept(typeof(AuditInterceptor))]`，该 action 才会进入拦截器链。

```csharp
using Microsoft.AspNetCore.Mvc;
using Tw.Castle.Core.Abstractions;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    [HttpPost("{orderId}")]
    [Intercept(typeof(AuditInterceptor))]
    public IActionResult Submit(string orderId)
    {
        return Ok(orderId);
    }
}
```

需要对整个 controller 生效时，可以把 `[Intercept(typeof(AuditInterceptor))]` 标注到 controller 类上。MVC adapter 复用 [`Tw.Castle.Core` 方法级动态代理拦截](../Tw.Castle.Core/method-interception.md)中的 `[Intercept]`、`[DisableInterception]`、`[InterceptorOrder]` 等选择语义。

## 标记 Razor Page handler

`TwPageInterceptionFilter` 把 Razor Page handler 接入同一套拦截 pipeline。`[Intercept]` 可以标注在 handler 方法或 page model 类上：

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tw.Castle.Core.Abstractions;

public sealed class OrderModel : PageModel
{
    [Intercept(typeof(AuditInterceptor))]
    public IActionResult OnPost(string orderId)
    {
        return Page();
    }
}
```

filter 解析当前 handler 的 `HandlerMethodDescriptor.MethodInfo` 并创建 `PageInvocationContext`，参数改写、短路与结果替换语义与 MVC action 一致：在 `ProceedAsync()` 前改写 `Arguments` 会回写到 `HandlerArguments`，设置 `IActionResult` 类型的 `ReturnValue` 会写入 page 的 `Result`。请求未命中具名 handler（页面没有匹配的 `OnGet`/`OnPost`）时，filter 直接继续 Razor Pages 管线，不进入拦截链。

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
using Tw.Castle.Core.Abstractions;

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

`AddMvcIntegration()` 注册 MVC action filter 与 Razor Page handler filter，适用于 controller action、Web API action 与 Razor Page handler。

以下入口不由本能力承载：

- Middleware。
- Minimal API。
- gRPC。

跨协议宿主启动入口仍使用 [`Tw.AspNetCore`](../Tw.AspNetCore/README.md) 的 `UseWebIntegration()`；MVC/Razor Pages AOP adapter 由 `Tw.AspNetCore.Mvc` 单独注册。
