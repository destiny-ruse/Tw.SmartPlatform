# Public API: Tw.Localization

标识：Tw.Localization / backend/dotnet/BuildingBlocks/src/Tw.Localization

公开能力边界：
- Tw.Localization.Context
- Tw.Localization.Text
- Tw.Localization.Json
- Tw.Localization.DynamicText
- Tw.Localization.EntityTranslation
- Tw.Localization.Caching

实现公开命名空间：
- Tw.Localization
- Tw.Localization.Caching
- Tw.Localization.Json
- Tw.Localization.Requests

公开类型：
- static class CultureFallback - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/CultureFallback.cs:8)
- sealed class DynamicTextContributor - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/DynamicTextContributor.cs:9)
- sealed record EntityTranslation - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/EntityTranslation.cs:12)
- sealed class EntityTranslationService - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/EntityTranslationService.cs:10)
- interface IDynamicTextStore - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/IDynamicTextStore.cs:9)
- interface IEntityTranslationService - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/IEntityTranslationService.cs:8)
- interface IEntityTranslationStore - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/IEntityTranslationStore.cs:8)
- interface IStaticTextSnapshot - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/IStaticTextSnapshot.cs:7)
- interface ITextLocalizer - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/ITextLocalizer.cs:7)
- interface ITextResourceContributor - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/ITextResourceContributor.cs:8)
- sealed class LanguageInfo - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/LanguageInfo.cs:6)
- sealed class LocalizationContext - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizationContext.cs:6)
- sealed class LocalizationOptions - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizationOptions.cs:8)
- static class LocalizationServiceCollectionExtensions - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizationServiceCollectionExtensions.cs:12)
- sealed record LocalizedText - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizedText.cs:12)
- enum LocalizedTextSource - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizedTextSource.cs:6)
- enum MissingTextBehavior - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/MissingTextBehavior.cs:6)
- sealed class StaticTextSnapshot - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/StaticTextSnapshot.cs:9)
- sealed class TextLocalizer - Tw.Localization (backend/dotnet/BuildingBlocks/src/Tw.Localization/TextLocalizer.cs:10)
- interface ILocalizationCacheInvalidator - Tw.Localization.Caching (backend/dotnet/BuildingBlocks/src/Tw.Localization/Caching/ILocalizationCacheInvalidator.cs:6)
- interface ILocalizationChangeToken - Tw.Localization.Caching (backend/dotnet/BuildingBlocks/src/Tw.Localization/Caching/ILocalizationChangeToken.cs:6)
- sealed record JsonTextResource - Tw.Localization.Json (backend/dotnet/BuildingBlocks/src/Tw.Localization/Json/JsonTextResource.cs:9)
- sealed class JsonTextResourceContributor - Tw.Localization.Json (backend/dotnet/BuildingBlocks/src/Tw.Localization/Json/JsonTextResourceContributor.cs:8)
- static class JsonTextResourceParser - Tw.Localization.Json (backend/dotnet/BuildingBlocks/src/Tw.Localization/Json/JsonTextResourceParser.cs:9)
- sealed record EntityTranslationBatchQuery - Tw.Localization.Requests (backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/EntityTranslationBatchQuery.cs:9)
- sealed record EntityTranslationKey - Tw.Localization.Requests (backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/EntityTranslationKey.cs:9)
- sealed record EntityTranslationLookup - Tw.Localization.Requests (backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/EntityTranslationLookup.cs:8)
- sealed record EntityTranslationQuery - Tw.Localization.Requests (backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/EntityTranslationQuery.cs:11)
- sealed record TextFillRequest - Tw.Localization.Requests (backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/TextFillRequest.cs:12)
- sealed record TextLookupRequest - Tw.Localization.Requests (backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/TextLookupRequest.cs:12)

DI 注册入口：
- Tw.Localization.LocalizationServiceCollectionExtensions.AddLocalization

使用文档：
- docs/shared-packages/dotnet/Tw.Localization/entity-translation.md
- docs/shared-packages/dotnet/Tw.Localization/README.md
- docs/shared-packages/dotnet/Tw.Localization/text-localization.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 使用文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Localization
