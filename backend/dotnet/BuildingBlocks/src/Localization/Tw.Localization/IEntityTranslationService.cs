using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>
/// 提供实体字段翻译的高层查询能力，支持单字段查找和批量字段查询
/// </summary>
public interface IEntityTranslationService
{
    /// <summary>
    /// 查找指定实体某个字段在目标文化下的翻译文本
    /// </summary>
    /// <param name="lookup">包含实体类型、实体标识、字段名称、文化名称及租户信息的单字段查找请求</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>找到翻译时返回翻译文本；否则返回 <see langword="null"/></returns>
    ValueTask<string?> GetFieldAsync(
        EntityTranslationLookup lookup,
        CancellationToken cancellationToken);

    /// <summary>
    /// 批量查找多个实体字段的翻译，以 <see cref="EntityTranslationKey"/> 为键返回结果字典
    /// </summary>
    /// <param name="query">包含多个实体字段查找请求的批量查询对象</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>以 <see cref="EntityTranslationKey"/> 为键、<see cref="EntityTranslation"/> 为值的只读字典；未找到的键不出现在字典中</returns>
    ValueTask<IReadOnlyDictionary<EntityTranslationKey, EntityTranslation>> GetFieldsAsync(
        EntityTranslationBatchQuery query,
        CancellationToken cancellationToken);
}
