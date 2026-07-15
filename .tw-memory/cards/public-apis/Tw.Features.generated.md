# Public API: Tw.Features

标识：Tw.Features / backend/dotnet/BuildingBlocks/src/Application/Tw.Features

公开能力边界：
- Tw.Features

实现公开命名空间：
- Tw.Features

公开类型：
- sealed record FeatureCacheKey - Tw.Features (backend/dotnet/BuildingBlocks/src/Application/Tw.Features/FeatureCacheKey.cs:9)
- sealed record FeatureCheckResult - Tw.Features (backend/dotnet/BuildingBlocks/src/Application/Tw.Features/FeatureCheckResult.cs:9)
- sealed record FeatureDefinition - Tw.Features (backend/dotnet/BuildingBlocks/src/Application/Tw.Features/FeatureDefinition.cs:8)
- sealed record FeatureRefreshRequest - Tw.Features (backend/dotnet/BuildingBlocks/src/Application/Tw.Features/FeatureRefreshRequest.cs:9)
- enum FeatureScope - Tw.Features (backend/dotnet/BuildingBlocks/src/Application/Tw.Features/FeatureScope.cs:6)
- sealed record FeatureValue - Tw.Features (backend/dotnet/BuildingBlocks/src/Application/Tw.Features/FeatureValue.cs:11)
- interface IFeatureCache - Tw.Features (backend/dotnet/BuildingBlocks/src/Application/Tw.Features/IFeatureCache.cs:6)
- interface IFeatureChecker - Tw.Features (backend/dotnet/BuildingBlocks/src/Application/Tw.Features/IFeatureChecker.cs:6)
- interface IFeatureStore - Tw.Features (backend/dotnet/BuildingBlocks/src/Application/Tw.Features/IFeatureStore.cs:6)

DI 注册入口：
- none

包参考文档：
- docs/shared-packages/dotnet/Tw.Features/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Features
