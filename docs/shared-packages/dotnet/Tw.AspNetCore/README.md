# Tw.AspNetCore

`Tw.AspNetCore` 提供 ASP.NET Core host-level 宿主启动聚合能力。本页按功能跳转到使用文档。

## 能力索引

- [宿主启动聚合入口](host-startup.md)：统一调用 `UseAutofac()`，并通过 Autofac native `ContainerBuilder.AddServiceRegistration(builder.Configuration)` 接入 `Tw.DependencyInjection`。

## 说明

MVC/Web API 专属能力（HTTP cancellation provider、MVC action AOP filter adapter）不内置于本包，归 [`Tw.AspNetCore.Mvc`](../Tw.AspNetCore.Mvc/README.md) 承载。

Web 本地化能力（请求文化解析、`IStringLocalizer` 适配）不内置于本包，刻意分离至独立的可选包 [`Tw.AspNetCore.Localization`](../Tw.AspNetCore.Localization/README.md)。`Tw.AspNetCore` 保持 host-level 宿主启动聚合职责，不承担本地化语义。
