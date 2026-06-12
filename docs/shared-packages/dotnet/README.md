# .NET 共享包

本页是 `backend/dotnet/BuildingBlocks/src` 下 .NET 共享包的使用文档索引，按包跳转。

## 包索引

- [Tw.Core](Tw.Core/README.md)：跨服务复用的基础原语与无框架依赖工具。
- [Tw.DependencyInjection](Tw.DependencyInjection/README.md)：框架绑定的依赖注入执行引擎，承载程序集扫描、容器接管、服务自动注册、注册规划诊断与 Options 自动装载。
- [Tw.AspNetCore](Tw.AspNetCore/README.md)：ASP.NET Core host-level 启动聚合能力，作为 Web API 与 gRPC 等宿主组合根入口。
- [Tw.AspNetCore.Mvc](Tw.AspNetCore.Mvc/README.md)：MVC/Web API 专属集成能力，提供 HTTP cancellation provider 以及 MVC action 与 Razor Page handler 的 AOP filter adapter。
- [Tw.AspNetCore.Grpc](Tw.AspNetCore.Grpc/README.md)：gRPC 服务端专属集成能力，注册 ASP.NET Core gRPC 服务端能力并明确 gRPC 原生 interceptor 边界。
- [Tw.Localization](Tw.Localization/README.md)：独立可选的多语言核心包，支持静态 JSON 资源、动态文本覆盖和实体字段翻译。
- [Tw.Localization.AspNetCore](Tw.Localization.AspNetCore/README.md)：`Tw.Localization` 的可选 ASP.NET Core Web 适配包，提供请求文化解析中间件、`IStringLocalizer` 适配器和运行时导出 DTO。
