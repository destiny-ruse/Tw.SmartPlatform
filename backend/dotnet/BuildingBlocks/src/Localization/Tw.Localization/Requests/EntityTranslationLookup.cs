namespace Tw.Localization.Requests;

/// <summary>
/// 表示查找单条实体字段翻译的请求，包含翻译键和本地化上下文
/// </summary>
/// <param name="Key">标识要查找的实体字段翻译的复合键</param>
/// <param name="Context">本次查找的本地化上下文，包含目标文化和租户信息</param>
public sealed record EntityTranslationLookup(
    EntityTranslationKey Key,
    LocalizationContext Context);
