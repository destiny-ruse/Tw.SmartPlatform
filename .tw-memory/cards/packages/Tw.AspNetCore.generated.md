# Package: Tw.AspNetCore

标识：Tw.AspNetCore / backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore / platform-team
职责：基于 WebApplicationBuilder 的跨协议宿主启动与聚合入口，统一承载 Web API 与 gRPC 等 ASP.NET Core 宿主在组合根中的 Microsoft DI 自动注册， 并提供认证方案、协议错误和请求关联等入口协议契约。

适用范围：
- HTTP 宿主治理
- 异常处理中间件
- 认证边界选项
- 认证方案名称契约
- 协议错误契约
- 请求关联契约
- 应用限流
- 健康检查端点
- 宿主启动聚合入口
- 依赖注入聚合

不适用范围：
- 与框架无关的基础原语
- 数据访问、ORM、仓储实现
- 具体业务领域模型
- HTTP 请求取消令牌提供器（归 Tw.AspNetCore.Mvc）
- MVC Filter、模型绑定与结果封装等 MVC 专属能力（归 Tw.AspNetCore.Mvc）
- 通用动态代理与 action 级 AOP 适配器
- 中间件适配器
- Minimal API endpoint filter 适配器
- gRPC interceptor 等 gRPC 专属能力
- 普通 Worker 与 Generic Host 专属启动入口

依赖边界：
- forbid: Microsoft.EntityFrameworkCore*, Autofac*, Castle.*, Tw.Castle.*, Tw.DependencyInjection.Autofac
- allow: Tw.DependencyInjection

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.AspNetCore
