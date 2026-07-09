namespace Tw.Localization.Requests;

/// <summary>
/// 表示在指定候选文化列表下批量查找多个实体字段翻译的请求
/// </summary>
/// <param name="Keys">要查找的实体字段翻译复合键列表</param>
/// <param name="Context">本次查找的本地化上下文，包含目标文化和租户信息</param>
/// <param name="CandidateCultureNames">
/// 按优先级排列的候选文化名称列表；查找时将按顺序依次尝试，直到找到匹配项
/// </param>
public sealed record EntityTranslationQuery(
    IReadOnlyList<EntityTranslationKey> Keys,
    LocalizationContext Context,
    IReadOnlyList<string> CandidateCultureNames);
