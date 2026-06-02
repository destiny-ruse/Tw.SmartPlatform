# Package: Tw.AspNetCore

标识：Tw.AspNetCore / backend/dotnet/BuildingBlocks/src/Tw.AspNetCore / platform-team
职责：ASP.NET Core 宿主集成的公共构建块：中间件、过滤器、模型绑定、 结果封装、启动扩展与 Web 层横切关注点。

适用范围：
- ASP.NET Core 中间件与过滤器
- Web 层模型绑定与结果封装
- 宿主启动与依赖注入扩展

不适用范围：
- 与框架无关的基础原语
- 数据访问、ORM、仓储实现
- 具体业务领域模型

依赖边界：
- forbid: Microsoft.EntityFrameworkCore*
- allow: 

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.AspNetCore
