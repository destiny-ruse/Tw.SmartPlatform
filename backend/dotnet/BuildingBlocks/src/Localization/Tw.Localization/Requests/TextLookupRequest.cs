namespace Tw.Localization.Requests;

/// <summary>
/// 表示查找单条本地化文本的请求，包含资源名称、键名、上下文及候选文化列表
/// </summary>
/// <param name="ResourceName">资源集合的名称，例如 "App"</param>
/// <param name="Name">文本条目的键名，例如 "Menu.Home"</param>
/// <param name="Context">本次查找的本地化上下文，包含目标文化和租户信息</param>
/// <param name="CandidateCultureNames">
/// 按优先级排列的候选文化名称列表；查找时将按顺序依次尝试，直到找到匹配项
/// </param>
public sealed record TextLookupRequest(
    string ResourceName,
    string Name,
    LocalizationContext Context,
    IReadOnlyList<string> CandidateCultureNames);
