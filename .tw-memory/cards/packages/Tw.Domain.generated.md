# Package: Tw.Domain

标识：Tw.Domain / backend/dotnet/BuildingBlocks/src/Application/Tw.Domain / platform-team
职责：提供不依赖 ORM 或数据库 provider 的实体审计、乐观并发与软删除形状契约。

适用范围：
- 实体创建与更新审计标记
- 实体并发戳与版本戳标记
- 实体软删除标记

不适用范围：
- 具体限界上下文的业务 DTO、共享枚举与领域契约
- 领域实体与聚合根基础类型
- 应用服务编排与 MediatR Handler
- 仓储、工作单元与数据访问实现

依赖边界：
- forbid: MediatR, FluentValidation, Microsoft.AspNetCore.*, SqlSugar*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Domain
