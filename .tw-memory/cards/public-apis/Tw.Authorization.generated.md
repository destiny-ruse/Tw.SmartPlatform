# Public API: Tw.Authorization

标识：Tw.Authorization / backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization

公开能力边界：
- Tw.Authorization

实现公开命名空间：
- Tw.Authorization

公开类型：
- sealed record AuthorizationContext - Tw.Authorization (backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/AuthorizationContext.cs:12)
- sealed record AuthorizationResult - Tw.Authorization (backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/AuthorizationResult.cs:9)
- interface IGrantStore - Tw.Authorization (backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/IGrantStore.cs:6)
- interface IPermissionChecker - Tw.Authorization (backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/IPermissionChecker.cs:6)
- interface IPermissionGrantCache - Tw.Authorization (backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/IPermissionGrantCache.cs:6)
- sealed class PermissionChecker - Tw.Authorization (backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/PermissionChecker.cs:6)
- sealed record PermissionDefinition - Tw.Authorization (backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/PermissionDefinition.cs:8)
- sealed record PermissionGrantCacheKey - Tw.Authorization (backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/PermissionGrantCacheKey.cs:11)

DI 注册入口：
- none

包参考文档：
- docs/shared-packages/dotnet/Tw.Authorization/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Authorization
