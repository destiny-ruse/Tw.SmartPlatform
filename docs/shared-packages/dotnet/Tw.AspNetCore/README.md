# Tw.AspNetCore

`Tw.AspNetCore` 提供 ASP.NET Core 宿主启动、入口协议契约、异常处理中间件、认证边界、限流和健康检查路由。包内能力保持提供方无关，不引入 gRPC、消息总线、数据库或具体基础设施实现。

## 能力索引

- [宿主启动聚合入口](host-startup.md)：使用 Microsoft DI 默认容器，并通过 `builder.Services.AddServiceRegistration(builder.Configuration)` 接入自动注册
- `Tw.AspNetCore.Authentication.AuthenticationSchemeNames`：提供 Bearer 与 Cookie 认证方案名称
- `Tw.AspNetCore.Errors.ProtocolError`：描述由入口适配器映射的 HTTP 状态码、稳定错误码、安全消息和追踪标识
- `Tw.AspNetCore.Correlation.RequestCorrelation`：传递链路追踪标识与业务关联标识
- `Tw.AspNetCore.Health.MapHealthEndpoint()`：映射单一 `/health` 端点

## 映射健康检查端点

在应用完成路由配置后调用 `MapHealthEndpoint()`：

```csharp
using Tw.AspNetCore.Health;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapHealthEndpoint();
app.Run();
```

该方法只映射 `/health`，并返回调用方传入的同一 `IEndpointRouteBuilder`，便于继续链式配置其他端点。

## 边界说明

- MVC 筛选器、模型绑定和结果封装归 [`Tw.AspNetCore.Mvc`](../Tw.AspNetCore.Mvc/README.md) 承载
- gRPC 拦截器与 gRPC 错误映射归 [`Tw.AspNetCore.Grpc`](../Tw.AspNetCore.Grpc/README.md) 承载
- 请求文化解析与 `IStringLocalizer` 适配归 [`Tw.AspNetCore.Localization`](../Tw.AspNetCore.Localization/README.md) 承载
- CAP 等消息协议错误由对应消息入口适配器负责映射
- 请求取消信号由控制器或端点在调用边界显式接收并向下游传递
