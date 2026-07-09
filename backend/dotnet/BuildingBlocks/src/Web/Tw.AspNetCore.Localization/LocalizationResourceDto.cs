namespace Tw.AspNetCore.Localization;

/// <summary>
/// 表示一个运行时导出的本地化资源集合，包含资源名称、文化名称及所有文本条目列表
/// </summary>
/// <param name="ResourceName">资源集合的名称，例如 "App"</param>
/// <param name="CultureName">BCP 47 文化名称，例如 "zh-Hans"</param>
/// <param name="Texts">当前资源集合下所有本地化文本条目的只读列表</param>
public sealed record LocalizationResourceDto(
    string ResourceName,
    string CultureName,
    IReadOnlyList<LocalizationTextDto> Texts);
