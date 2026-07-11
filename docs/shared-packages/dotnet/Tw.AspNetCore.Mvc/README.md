# Tw.AspNetCore.Mvc

`Tw.AspNetCore.Mvc` 提供 ASP.NET Core MVC 与 Web API 专属集成能力。本页按功能跳转到使用文档。

## 能力范围

- MVC 模型绑定、统一 API 响应契约、API Versioning URL Segment 注册与 CSRF 防伪校验策略。

## 边界

`Tw.AspNetCore.Mvc` 承载 MVC/Web API 专属适配能力。跨协议宿主启动入口仍归 [`Tw.AspNetCore`](../Tw.AspNetCore/README.md)。MVC action 与 Razor Page 的横切关注点应使用原生 MVC filter；本包不提供通用动态代理或统一拦截 pipeline。

本包不承载 Middleware、Minimal API 或 gRPC adapter。
