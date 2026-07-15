# Package: Tw.AspNetCore.Localization

标识：Tw.AspNetCore.Localization / backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization / platform-team
职责：独立可选的 ASP.NET Core 多语言适配构建块：请求语言解析、当前本地化上下文、 IStringLocalizer 适配、Web 侧服务注册和运行时本地化资源导出 DTO 契约。

适用范围：
- ASP.NET Core 本地化命名空间
- 请求区域性中间件
- 本地化上下文访问器
- ASP.NET Core 请求语言解析中间件
- Web 请求本地化上下文访问器
- IStringLocalizer 与 IStringLocalizer<T> 适配
- Web 多语言依赖注入入口
- 运行时本地化资源导出 DTO 契约

不适用范围：
- 多语言核心模型和回退编排
- EF Core 表模型、DbContext、迁移或默认数据库实现
- 管理端页面和管理 API
- 具体业务领域模型

依赖边界：
- forbid: Microsoft.EntityFrameworkCore*
- allow: Tw.Core, Tw.Localization, Microsoft.AspNetCore.App

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.AspNetCore.Localization
