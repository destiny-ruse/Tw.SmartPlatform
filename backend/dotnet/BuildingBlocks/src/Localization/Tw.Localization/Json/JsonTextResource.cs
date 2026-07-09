namespace Tw.Localization.Json;

/// <summary>
/// 表示从单个 JSON 文件解析出的本地化文本资源，包含资源名称、文化名称及扁平化键值对
/// </summary>
/// <param name="ResourceName">资源集合的名称，例如 "App"</param>
/// <param name="CultureName">BCP 47 文化名称，例如 "zh-Hans"</param>
/// <param name="Texts">以 "__" 分隔的扁平化键值对字典</param>
public sealed record JsonTextResource(
    string ResourceName,
    string CultureName,
    IReadOnlyDictionary<string, string> Texts);
