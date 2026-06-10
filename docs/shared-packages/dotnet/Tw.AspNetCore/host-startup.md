# 启用宿主启动聚合入口

本指南面向使用 `Tw.AspNetCore` 的 .NET 开发者，目标是在应用组合根中启用统一宿主启动入口。

## 在组合根调用入口

在创建 `WebApplicationBuilder` 后调用一次 `UseTwHostStartup()`：

```csharp
using Tw.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.UseTwHostStartup();

var app = builder.Build();
app.Run();
```

`UseTwHostStartup()` 是组合根入口，应在应用启动配置中调用一次。该入口适用于使用 `WebApplicationBuilder` 的 Web API 与 gRPC 等 ASP.NET Core 宿主组合根。

## 入口聚合的能力

`UseTwHostStartup()` 聚合以下宿主启动动作：

- 调用 `builder.Host.UseAutofac()`，由 Autofac 接管宿主容器。
- 通过 Autofac native `ContainerBuilder.AddServiceRegistration(builder.Configuration)` 路径接入 `Tw.DependencyInjection`。
- 启用 `Tw.DependencyInjection` 的服务、Options 与 AOP 自动注册能力。

## 边界

该入口只负责基于 `WebApplicationBuilder` 的跨协议宿主启动聚合。MVC Filter 与 gRPC 专属 interceptor 不在 P5 宿主入口内实现，由对应包和计划承载。普通 Worker 与 Generic Host 专属入口不由当前 API 表达。

该入口不迁移 `HttpContextCancellationTokenProvider`；该 provider 仍按现有文档独立使用。
