# Public API: Tw.Localization

标识：Tw.Localization / backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization

公开能力边界：
- Tw.Localization
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
- static class CultureFallback - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/CultureFallback.cs:8)
- sealed class DynamicTextContributor - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/DynamicTextContributor.cs:9)
- sealed record EntityTranslation - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/EntityTranslation.cs:12)
- sealed class EntityTranslationService - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/EntityTranslationService.cs:9)
- interface IDynamicTextStore - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/IDynamicTextStore.cs:9)
- interface IEntityTranslationService - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/IEntityTranslationService.cs:8)
- interface IEntityTranslationStore - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/IEntityTranslationStore.cs:8)
- interface IStaticTextSnapshot - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/IStaticTextSnapshot.cs:7)
- interface ITextLocalizer - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/ITextLocalizer.cs:7)
- interface ITextResourceContributor - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/ITextResourceContributor.cs:8)
- sealed class LanguageInfo - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/LanguageInfo.cs:6)
- class LocalizationConfigurationException - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/LocalizationConfigurationException.cs:8)
- sealed class LocalizationContext - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/LocalizationContext.cs:6)
- sealed class LocalizationOptions - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/LocalizationOptions.cs:6)
- static class LocalizationServiceCollectionExtensions - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/LocalizationServiceCollectionExtensions.cs:10)
- sealed record LocalizedText - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/LocalizedText.cs:12)
- enum LocalizedTextSource - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/LocalizedTextSource.cs:6)
- enum MissingTextBehavior - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/MissingTextBehavior.cs:6)
- sealed class StaticTextSnapshot - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/StaticTextSnapshot.cs:9)
- sealed class TextLocalizer - Tw.Localization (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/TextLocalizer.cs:9)
- interface ILocalizationCacheInvalidator - Tw.Localization.Caching (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Caching/ILocalizationCacheInvalidator.cs:6)
- interface ILocalizationChangeToken - Tw.Localization.Caching (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Caching/ILocalizationChangeToken.cs:6)
- sealed record JsonTextResource - Tw.Localization.Json (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Json/JsonTextResource.cs:9)
- sealed class JsonTextResourceContributor - Tw.Localization.Json (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Json/JsonTextResourceContributor.cs:8)
- static class JsonTextResourceParser - Tw.Localization.Json (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Json/JsonTextResourceParser.cs:8)
- sealed record EntityTranslationBatchQuery - Tw.Localization.Requests (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Requests/EntityTranslationBatchQuery.cs:9)
- sealed record EntityTranslationKey - Tw.Localization.Requests (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Requests/EntityTranslationKey.cs:9)
- sealed record EntityTranslationLookup - Tw.Localization.Requests (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Requests/EntityTranslationLookup.cs:8)
- sealed record EntityTranslationQuery - Tw.Localization.Requests (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Requests/EntityTranslationQuery.cs:11)
- sealed record TextFillRequest - Tw.Localization.Requests (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Requests/TextFillRequest.cs:12)
- sealed record TextLookupRequest - Tw.Localization.Requests (backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Requests/TextLookupRequest.cs:12)

DI 注册入口：
- Tw.Localization.LocalizationServiceCollectionExtensions.AddLocalization

包参考文档：
- docs/shared-packages/dotnet/Tw.Localization/entity-translation.md
- docs/shared-packages/dotnet/Tw.Localization/README.md
- docs/shared-packages/dotnet/Tw.Localization/text-localization.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Localization
