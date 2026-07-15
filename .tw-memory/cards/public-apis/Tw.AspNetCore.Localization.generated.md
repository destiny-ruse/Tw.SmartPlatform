# Public API: Tw.AspNetCore.Localization

标识：Tw.AspNetCore.Localization / backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization

公开能力边界：
- Tw.AspNetCore.Localization

实现公开命名空间：
- Tw.AspNetCore.Localization

公开类型：
- sealed class CurrentLocalizationContextAccessor - Tw.AspNetCore.Localization (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/CurrentLocalizationContextAccessor.cs:8)
- interface ICurrentLocalizationContextAccessor - Tw.AspNetCore.Localization (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/ICurrentLocalizationContextAccessor.cs:11)
- static class LocalizationApplicationBuilderExtensions - Tw.AspNetCore.Localization (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/LocalizationApplicationBuilderExtensions.cs:8)
- static class LocalizationServiceCollectionExtensions - Tw.AspNetCore.Localization (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/LocalizationServiceCollectionExtensions.cs:12)
- static class RequestCultureResolver - Tw.AspNetCore.Localization (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/RequestCultureResolver.cs:8)
- sealed class RequestLocalizationMiddleware - Tw.AspNetCore.Localization (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/RequestLocalizationMiddleware.cs:16)
- sealed class StaticSnapshotStringLocalizer - Tw.AspNetCore.Localization (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/StaticSnapshotStringLocalizer.cs:15)
- sealed class StaticSnapshotStringLocalizer - Tw.AspNetCore.Localization (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/StaticSnapshotStringLocalizerOfT.cs:9)
- sealed class StaticSnapshotStringLocalizerFactory - Tw.AspNetCore.Localization (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/StaticSnapshotStringLocalizerFactory.cs:10)

DI 注册入口：
- Tw.AspNetCore.Localization.LocalizationServiceCollectionExtensions.AddLocalization

包参考文档：
- docs/shared-packages/dotnet/Tw.AspNetCore.Localization/README.md
- docs/shared-packages/dotnet/Tw.AspNetCore.Localization/request-localization.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.AspNetCore.Localization
