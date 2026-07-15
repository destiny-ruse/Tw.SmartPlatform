# Public API: Tw.Settings

标识：Tw.Settings / backend/dotnet/BuildingBlocks/src/Application/Tw.Settings

公开能力边界：
- Tw.Settings

实现公开命名空间：
- Tw.Settings

公开类型：
- interface ISettingCache - Tw.Settings (backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/ISettingCache.cs:6)
- interface ISettingProvider - Tw.Settings (backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/ISettingProvider.cs:6)
- interface ISettingStore - Tw.Settings (backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/ISettingStore.cs:6)
- sealed record SettingCacheKey - Tw.Settings (backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/SettingCacheKey.cs:9)
- sealed record SettingDefinition - Tw.Settings (backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/SettingDefinition.cs:8)
- sealed record SettingRefreshRequest - Tw.Settings (backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/SettingRefreshRequest.cs:9)
- enum SettingScope - Tw.Settings (backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/SettingScope.cs:6)
- sealed record SettingValue - Tw.Settings (backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/SettingValue.cs:11)

DI 注册入口：
- none

包参考文档：
- docs/shared-packages/dotnet/Tw.Settings/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Settings
