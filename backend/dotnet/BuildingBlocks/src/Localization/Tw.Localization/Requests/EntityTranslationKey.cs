namespace Tw.Localization.Requests;

/// <summary>
/// 唯一标识实体某个字段翻译条目的复合键，使用值相等语义进行比较
/// </summary>
/// <param name="EntityType">实体的类型名称，例如 "Product"</param>
/// <param name="EntityId">实体的唯一标识，例如 "42"</param>
/// <param name="FieldName">被翻译的字段名称，例如 "Name"</param>
public sealed record EntityTranslationKey(string EntityType, string EntityId, string FieldName);
