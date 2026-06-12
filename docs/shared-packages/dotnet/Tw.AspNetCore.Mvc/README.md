# Tw.AspNetCore.Mvc

`Tw.AspNetCore.Mvc` 提供 ASP.NET Core MVC 与 Web API 专属集成能力。本页按功能跳转到使用文档。

## 能力索引

- [启用 MVC action 与 Razor Page handler 拦截](mvc-interception.md)：通过 `AddMvcIntegration()` 将 MVC controller action 与 Razor Page handler 接入统一 `IInterceptorPipeline`。
- [HttpContext 取消令牌 Provider](context/http-context-cancellation-token-provider.md)：基于 `HttpContext.RequestAborted` 的请求取消令牌适配。

## 边界

`Tw.AspNetCore.Mvc` 承载 MVC/Web API 专属适配能力。跨协议宿主启动入口仍归 [`Tw.AspNetCore`](../Tw.AspNetCore/README.md)，方法级拦截核心抽象与 pipeline 承载仍归 [`Tw.Core`](../Tw.Core/README.md) 和 [`Tw.DependencyInjection`](../Tw.DependencyInjection/README.md)。

本包不承载 Middleware、Minimal API 或 gRPC adapter。
