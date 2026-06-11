# .NET 共享包

本页是 `backend/dotnet/BuildingBlocks/src` 下 .NET 共享包的使用文档索引，按包跳转。

## 包索引

- [Tw.Core](Tw.Core/README.md)：跨服务复用的基础原语与无框架依赖工具。
- [Tw.DependencyInjection](Tw.DependencyInjection/README.md)：框架绑定的依赖注入执行引擎，承载程序集扫描、容器接管、服务自动注册、注册规划诊断与 Options 自动装载。
- [Tw.AspNetCore](Tw.AspNetCore/README.md)：ASP.NET Core host-level 启动聚合能力。
- [Tw.AspNetCore.Mvc](Tw.AspNetCore.Mvc/README.md)：MVC/Web API 专属集成能力，提供 HTTP cancellation provider 与 MVC action AOP filter adapter。
- [Tw.Localization](Tw.Localization/README.md)：独立可选的多语言核心包，支持静态 JSON 资源、动态文本覆盖和实体字段翻译。
- [Tw.Localization.AspNetCore](Tw.Localization.AspNetCore/README.md)：`Tw.Localization` 的可选 ASP.NET Core Web 适配包，提供请求文化解析中间件、`IStringLocalizer` 适配器和运行时导出 DTO。
