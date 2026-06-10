# Tw.AspNetCore

`Tw.AspNetCore` 提供 ASP.NET Core 宿主集成能力。本页按功能跳转到使用文档。

## 能力索引

- [宿主启动聚合入口](host-startup.md)：统一调用 `UseAutofac()`，并通过 Autofac native `ContainerBuilder.AddServiceRegistration(builder.Configuration)` 接入 `Tw.DependencyInjection`。
- [HttpContext 取消令牌 Provider](context/http-context-cancellation-token-provider.md)：基于 `HttpContext.RequestAborted` 的请求取消令牌适配。

## 说明

Web 本地化能力（请求文化解析、`IStringLocalizer` 适配）不内置于本包，刻意分离至独立的可选包 [`Tw.Localization.AspNetCore`](../Tw.Localization.AspNetCore/README.md)。`Tw.AspNetCore` 保持 host-level 宿主启动聚合职责，不承担本地化语义。
