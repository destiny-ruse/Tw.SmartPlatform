# Package: Tw.AspNetCore.Mvc

标识：Tw.AspNetCore.Mvc / backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc / platform-team
职责：ASP.NET Core MVC 与 Web API 专属集成构建块，承载模型绑定、 响应契约、API 版本与安全策略。

适用范围：
- MVC 模型绑定错误
- 统一 API 响应契约
- API Versioning URL Segment 注册
- CSRF 与防伪校验策略
- MVC 集成依赖注入入口

不适用范围：
- 跨协议 ASP.NET Core 宿主启动聚合入口
- 环境式取消令牌与 HTTP 请求取消令牌 provider
- 中间件适配器
- Minimal API endpoint filter 适配器
- gRPC interceptor 与 gRPC 专属能力
- 通用动态代理与 MVC action 或 Razor Page handler AOP 适配器
- 与框架无关的基础原语
- 数据访问、ORM、仓储实现

依赖边界：
- forbid: Microsoft.EntityFrameworkCore*, Autofac*, Castle.*, Tw.Castle.*, Tw.DependencyInjection.Autofac
- allow: Asp.Versioning.Mvc, Asp.Versioning.Mvc.ApiExplorer, Tw.Core

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.AspNetCore.Mvc
