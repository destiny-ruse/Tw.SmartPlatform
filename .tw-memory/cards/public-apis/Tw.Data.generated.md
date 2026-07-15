# Public API: Tw.Data

标识：Tw.Data / backend/dotnet/BuildingBlocks/src/Data/Tw.Data

公开能力边界：
- Tw.Data
- Tw.Data.Uow

实现公开命名空间：
- Tw.Data.Uow

公开类型：
- interface IOutboxTransactionBoundary - Tw.Data.Uow (backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Uow/IOutboxTransactionBoundary.cs:10)
- interface IUnitOfWork - Tw.Data.Uow (backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Uow/IUnitOfWork.cs:11)
- interface IUnitOfWorkCoordinator - Tw.Data.Uow (backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Uow/IUnitOfWorkCoordinator.cs:6)
- sealed record UnitOfWorkOptions - Tw.Data.Uow (backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Uow/UnitOfWorkOptions.cs:29)
- enum UnitOfWorkScope - Tw.Data.Uow (backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Uow/UnitOfWorkOptions.cs:6)
- enum UnitOfWorkTransactionBehavior - Tw.Data.Uow (backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Uow/UnitOfWorkTransactionBehavior.cs:6)

DI 注册入口：
- none

包参考文档：
- docs/shared-packages/dotnet/Tw.Data/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Data
