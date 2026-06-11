# Tw.AspNetCore.Grpc

`Tw.AspNetCore.Grpc` 提供 ASP.NET Core gRPC 服务端专属集成能力。本页按功能跳转到使用文档。

## 能力索引

- [启用 gRPC 服务端集成](grpc-integration.md)：通过 `AddGrpcIntegration()` 注册 ASP.NET Core gRPC 服务端能力，并明确 gRPC 横切能力使用 gRPC 原生 interceptor。

## 边界

`Tw.AspNetCore.Grpc` 只承载 gRPC 服务端集成入口与 gRPC 原生 interceptor 使用边界。跨协议宿主启动入口仍归 [`Tw.AspNetCore`](../Tw.AspNetCore/README.md)；MVC/Web API action filter adapter 与 HTTP cancellation provider 仍归 [`Tw.AspNetCore.Mvc`](../Tw.AspNetCore.Mvc/README.md)。

本包不承载统一 `IInterceptorPipeline` adapter、MVC Filter、HTTP Middleware、Minimal API、业务 proto 契约或 Razor/MVC 能力。
