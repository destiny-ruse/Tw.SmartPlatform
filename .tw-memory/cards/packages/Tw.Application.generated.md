# Package: Tw.Application

标识：Tw.Application / backend/dotnet/BuildingBlocks/src/Application/Tw.Application / platform-team
职责：应用层用例执行管线、MediatR 集成、pipeline behavior 编排和 completed hook 执行。

适用范围：
- 应用 pipeline 顺序
- 用例执行器
- completed hook
- MediatR 应用层集成

不适用范围：
- HTTP Controller
- 数据访问实现
- Identity Center 实现

依赖边界：
- forbid: Microsoft.AspNetCore.*, SqlSugar*
- allow: Tw.Core, Tw.Application.Contracts, MediatR, FluentValidation, Microsoft.Extensions.DependencyInjection.Abstractions

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Application
