namespace Tw.Localization;

/// <summary>
/// 表示实体某个字段在特定文化下的一条翻译记录
/// </summary>
/// <param name="EntityType">实体的类型名称，例如 "Product"</param>
/// <param name="EntityId">实体的唯一标识，例如 "42"</param>
/// <param name="FieldName">被翻译的字段名称，例如 "Name"</param>
/// <param name="CultureName">翻译所属的 BCP 47 文化名称，例如 "zh-Hans"</param>
/// <param name="Value">该字段在对应文化下的翻译文本</param>
/// <param name="TenantId">翻译所属的租户标识；为 <see langword="null"/> 时表示全局翻译</param>
public sealed record EntityTranslation(
    string EntityType,
    string EntityId,
    string FieldName,
    string CultureName,
    string Value,
    string? TenantId);
