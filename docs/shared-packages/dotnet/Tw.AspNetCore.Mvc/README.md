# Tw.AspNetCore.Mvc

`Tw.AspNetCore.Mvc` 提供 ASP.NET Core MVC 与 Web API 专属集成能力。本页按功能跳转到使用文档。

## 能力索引

- [HttpContext 取消令牌 Provider](context/http-context-cancellation-token-provider.md)：基于 `HttpContext.RequestAborted` 的请求取消令牌适配。

## 边界

`Tw.AspNetCore.Mvc` 承载 MVC/Web API 专属适配能力。跨协议宿主启动入口仍归 [`Tw.AspNetCore`](../Tw.AspNetCore/README.md)。MVC action 与 Razor Page 的横切关注点应使用原生 MVC filter；本包不提供通用动态代理或统一拦截 pipeline。

本包不承载 Middleware、Minimal API 或 gRPC adapter。
