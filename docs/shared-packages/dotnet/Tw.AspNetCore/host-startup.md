# 启用宿主启动聚合入口

本指南面向使用 `Tw.AspNetCore` 的 .NET 开发者，目标是在应用组合根中启用统一宿主启动入口。

## 在组合根调用入口

在创建 `WebApplicationBuilder` 后调用一次 `UseWebIntegration()`：

```csharp
using Tw.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.UseWebIntegration();

var app = builder.Build();
app.Run();
```

`UseWebIntegration()` 是组合根入口，应在应用启动配置中调用一次。该入口适用于使用 `WebApplicationBuilder` 的 Web API 与 gRPC 等 ASP.NET Core 宿主组合根。

## 入口聚合的能力

`UseWebIntegration()` 聚合以下宿主启动动作：

- 通过 `builder.Services.AddServiceRegistration(builder.Configuration)` 执行 Microsoft DI 自动服务注册。
- 自动装载服务注册与 Options 诊断报告。

## 横切关注点

Microsoft DI 是默认容器，`UseWebIntegration()` 不接管其他容器，也不注册通用动态代理。横切关注点按宿主框架的原生扩展点实现：

- HTTP 请求处理使用 middleware。
- 认证与授权使用 policy。
- MVC/Web API 使用 MVC filter。
- Minimal API 使用 endpoint filter。
- gRPC 使用 server interceptor。
- CAP 使用 filter。
- Quartz 使用 listener。
- 应用启动或请求处理顺序使用应用管线。

## 边界

该入口只负责基于 `WebApplicationBuilder` 的跨协议宿主启动聚合。MVC filter 与 gRPC 专属 interceptor 不由宿主入口注册。普通 Worker 与 Generic Host 专属入口不由当前 API 表达。

MVC controller action 与 endpoint 应显式接收 `CancellationToken`；无法由框架绑定时，在 HTTP 边界读取 `HttpContext.RequestAborted` 后显式向下游传递。
