namespace Tw.Localization.Requests;

/// <summary>
/// 表示在固定上下文下批量查找多个实体字段翻译的请求；
/// 候选文化由上下文中的策略（回退父文化、回退默认文化）决定，无需显式传入候选列表
/// </summary>
/// <param name="Keys">要查找的实体字段翻译复合键列表</param>
/// <param name="Context">本次批量查找共用的本地化上下文，包含目标文化和租户信息</param>
public sealed record EntityTranslationBatchQuery(
    IReadOnlyList<EntityTranslationKey> Keys,
    LocalizationContext Context);
