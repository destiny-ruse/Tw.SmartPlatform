# 启用 gRPC 服务端集成

本指南面向使用 `Tw.AspNetCore.Grpc` 的 .NET 开发者，目标是在 ASP.NET Core gRPC 应用中注册 gRPC 服务端能力，并按 gRPC 原生模型接入 interceptor。

## 注册 gRPC integration

在组合根中调用宿主启动入口后，注册 gRPC integration：

```csharp
using Tw.AspNetCore;
using Tw.AspNetCore.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.UseTwHostStartup();
builder.Services.AddGrpcIntegration();
```

`AddGrpcIntegration()` 会调用 ASP.NET Core gRPC 原生 `AddGrpc()`，注册 gRPC 服务端能力。该入口不注册 MVC Filter、HTTP Middleware、Minimal API endpoint filter，也不把 gRPC 调用接入统一 `IInterceptorPipeline`。

## 使用 gRPC 原生 interceptor

gRPC 横切能力直接实现 gRPC 原生 interceptor：

```csharp
public sealed class AuditGrpcInterceptor : Grpc.Core.Interceptors.Interceptor
{
}
```

注册 interceptor 时使用 ASP.NET Core gRPC 原生方式：

```csharp
using Tw.AspNetCore;
using Tw.AspNetCore.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.UseTwHostStartup();
builder.Services.AddGrpc(options => options.Interceptors.Add<AuditGrpcInterceptor>());
builder.Services.AddGrpcIntegration();
```

也可以在 `AddGrpcIntegration()` 后按 ASP.NET Core gRPC 官方方式继续配置 `GrpcServiceOptions`。关键边界是：gRPC interceptor 由 ASP.NET Core gRPC 原生管线执行，不通过本仓库统一 AOP pipeline 转接。

## 与统一 AOP pipeline 的关系

`Tw.DynamicProxy.Abstractions.IInterceptor`、`IInterceptorPipeline`、Castle 动态代理和 MVC 方法级拦截器不进入 gRPC 调用链。需要为 gRPC 请求增加审计、日志、异常转换或指标时，应实现 `Grpc.Core.Interceptors.Interceptor`，并按 ASP.NET Core gRPC 的服务端 interceptor 规则注册。

MVC/Web API action 级拦截仍使用 [`Tw.AspNetCore.Mvc`](../Tw.AspNetCore.Mvc/README.md) 的 `AddMvcIntegration()`；跨协议宿主启动仍使用 [`Tw.AspNetCore`](../Tw.AspNetCore/README.md) 的 `UseTwHostStartup()`。

## 契约与协议边界

业务 proto 契约不属于 `Tw.AspNetCore.Grpc`。跨服务复用的 proto 应放在 `contracts/protos`；只服务单一业务服务的 proto 应留在该服务自己的契约边界内。

Middleware、Minimal API、MVC Filter、Razor Page handler 和 MVC/Web API 结果封装不属于本包。

## 验证与依赖边界

P7 验证中使用 `rg` 检查源码和测试时排除 `packages.lock.json`。lock 文件中可能出现由 host 或其他包传递带入的 Castle 依赖记录；`Tw.AspNetCore.Grpc` 的真实边界以源码和测试不引用 AOP、MVC、Castle adapter 为准。
