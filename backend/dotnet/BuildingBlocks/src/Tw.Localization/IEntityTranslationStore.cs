using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>
/// 提供对实体翻译持久化存储的查询访问能力
/// </summary>
public interface IEntityTranslationStore
{
    /// <summary>
    /// 根据查询条件批量获取实体翻译记录列表
    /// </summary>
    /// <param name="query">包含实体类型、实体标识、字段名称、文化名称及租户信息的查询条件</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>匹配查询条件的 <see cref="EntityTranslation"/> 只读列表；无结果时返回空列表</returns>
    ValueTask<IReadOnlyList<EntityTranslation>> GetListAsync(
        EntityTranslationQuery query,
        CancellationToken cancellationToken = default);
}
