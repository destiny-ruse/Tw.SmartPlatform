namespace Tw.Localization.Requests;

/// <summary>
/// 表示批量填充某个资源集合下所有文本条目的请求，包含资源名称、上下文及候选文化列表
/// </summary>
/// <param name="ResourceName">资源集合的名称，例如 "App"</param>
/// <param name="Context">本次填充的本地化上下文，包含目标文化和租户信息</param>
/// <param name="CandidateCultureNames">
/// 按优先级从高到低排列的候选文化名称列表；批量填充时作为回退链应用于整个文本集合，
/// 高优先级文化的条目覆盖低优先级文化的同名条目，所有文化的条目均会合并到结果中
/// </param>
public sealed record TextFillRequest(
    string ResourceName,
    LocalizationContext Context,
    IReadOnlyList<string> CandidateCultureNames);
