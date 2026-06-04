namespace Tw.Localization.Requests;

/// <summary>
/// 表示批量填充某个资源集合下所有文本条目的请求，包含资源名称、上下文及候选文化列表
/// </summary>
/// <param name="ResourceName">资源集合的名称，例如 "App"</param>
/// <param name="Context">本次填充的本地化上下文，包含目标文化和租户信息</param>
/// <param name="CandidateCultureNames">
/// 按优先级排列的候选文化名称列表；填充时将按顺序依次尝试，直到找到匹配项
/// </param>
public sealed record TextFillRequest(
    string ResourceName,
    LocalizationContext Context,
    IReadOnlyList<string> CandidateCultureNames);
