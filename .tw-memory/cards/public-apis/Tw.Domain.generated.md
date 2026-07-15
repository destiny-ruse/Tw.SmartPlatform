# Public API: Tw.Domain

标识：Tw.Domain / backend/dotnet/BuildingBlocks/src/Application/Tw.Domain

公开能力边界：
- Tw.Domain.Auditing
- Tw.Domain.Concurrency
- Tw.Domain.SoftDelete

实现公开命名空间：
- Tw.Domain.Auditing
- Tw.Domain.Concurrency
- Tw.Domain.SoftDelete

公开类型：
- interface IAuditedEntity - Tw.Domain.Auditing (backend/dotnet/BuildingBlocks/src/Application/Tw.Domain/Auditing/IAuditedEntity.cs:6)
- interface IHasConcurrencyStamp - Tw.Domain.Concurrency (backend/dotnet/BuildingBlocks/src/Application/Tw.Domain/Concurrency/IHasConcurrencyStamp.cs:6)
- interface IHasVersionStamp - Tw.Domain.Concurrency (backend/dotnet/BuildingBlocks/src/Application/Tw.Domain/Concurrency/IHasVersionStamp.cs:6)
- interface ISoftDelete - Tw.Domain.SoftDelete (backend/dotnet/BuildingBlocks/src/Application/Tw.Domain/SoftDelete/ISoftDelete.cs:6)

DI 注册入口：
- none

包参考文档：
- docs/shared-packages/dotnet/Tw.Domain/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Domain
