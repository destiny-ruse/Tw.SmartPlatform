# Public API: Tw.Localization.AspNetCore

标识：Tw.Localization.AspNetCore / backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore

公开能力边界：
- Tw.Localization.AspNetCore

实现公开命名空间：
- Tw.Localization.AspNetCore

公开类型：
- sealed class CurrentLocalizationContextAccessor - Tw.Localization.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/CurrentLocalizationContextAccessor.cs:8)
- interface ICurrentLocalizationContextAccessor - Tw.Localization.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/ICurrentLocalizationContextAccessor.cs:11)
- static class LocalizationApplicationBuilderExtensions - Tw.Localization.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationApplicationBuilderExtensions.cs:8)
- sealed record LocalizationResourceDto - Tw.Localization.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationResourceDto.cs:9)
- static class LocalizationServiceCollectionExtensions - Tw.Localization.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationServiceCollectionExtensions.cs:19)
- sealed record LocalizationTextDto - Tw.Localization.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationTextDto.cs:9)
- sealed record RequestCultureResolveResult - Tw.Localization.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/RequestCultureResolveResult.cs:11)
- static class RequestCultureResolver - Tw.Localization.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/RequestCultureResolver.cs:8)
- sealed class RequestLocalizationMiddleware - Tw.Localization.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/RequestLocalizationMiddleware.cs:16)
- sealed class TwStringLocalizer - Tw.Localization.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/TwStringLocalizer.cs:16)
- sealed class TwStringLocalizer - Tw.Localization.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/TwStringLocalizerOfT.cs:14)
- sealed class TwStringLocalizerFactory - Tw.Localization.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/TwStringLocalizerFactory.cs:16)

DI 注册入口：
- Tw.Localization.AspNetCore.LocalizationServiceCollectionExtensions.AddLocalization

使用文档：
- docs/shared-packages/dotnet/Tw.Localization.AspNetCore/README.md
- docs/shared-packages/dotnet/Tw.Localization.AspNetCore/request-localization.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 使用文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Localization.AspNetCore
