# Package: Tw.Localization

标识：Tw.Localization / backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization / platform-team
职责：独立可选的多语言核心构建块：语言上下文、系统文案资源、JSON 静态资源、 动态文案覆盖抽象、业务实体字段翻译抽象、回退链与本地化缓存失效契约。

适用范围：
- 系统文案本地化核心抽象与默认编排
- JSON 静态多语言资源解析与贡献源
- 本地化配置异常
- 动态系统文案 store 接口
- 业务实体翻译 store 接口与服务
- 多租户与 culture 回退策略

不适用范围：
- ASP.NET Core 请求语言解析和 IStringLocalizer 适配
- 环境式取消令牌与请求上下文传递
- EF Core 表模型、DbContext、迁移或默认数据库实现
- 管理端页面和管理 API
- 具体业务领域模型

依赖边界：
- forbid: Microsoft.AspNetCore.*, Microsoft.EntityFrameworkCore*
- allow: Tw.Core, Microsoft.Extensions.DependencyInjection.Abstractions

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Localization
