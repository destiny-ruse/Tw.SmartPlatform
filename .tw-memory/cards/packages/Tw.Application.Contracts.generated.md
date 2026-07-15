# Package: Tw.Application.Contracts

标识：Tw.Application.Contracts / backend/dotnet/BuildingBlocks/src/Application/Tw.Application.Contracts / platform-team
职责：提供不依赖 MediatR、FluentValidation 或基础设施 provider 的应用命令、查询标记与通用分页契约。

适用范围：
- Command 标记接口
- Query 标记接口
- 通用分页请求与结果模型

不适用范围：
- 具体限界上下文的业务 DTO、共享枚举与服务契约
- MediatR Handler 与应用 pipeline
- FluentValidation validator
- UoW 编排与权限检查执行
- 数据访问或协议 provider 集成

依赖边界：
- forbid: MediatR, FluentValidation, Microsoft.AspNetCore.*, SqlSugar*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Application.Contracts
