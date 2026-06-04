using Tw.Context;
using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>
/// 实体字段翻译服务，实现 <see cref="IEntityTranslationService"/>。
/// 通过 <see cref="IEntityTranslationStore"/> 批量查询，并按文化/租户优先级选出最佳翻译
/// </summary>
public sealed class EntityTranslationService : IEntityTranslationService
{
    private readonly IEntityTranslationStore _store;
    private readonly LocalizationOptions _options;
    private readonly ICancellationTokenProvider _cancellationTokenProvider;

    /// <summary>
    /// 初始化 <see cref="EntityTranslationService"/> 类的新实例
    /// </summary>
    /// <param name="store">实体翻译持久化存储</param>
    /// <param name="options">系统级本地化配置</param>
    /// <param name="cancellationTokenProvider">取消令牌 provider</param>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="store"/>、<paramref name="options"/> 或 <paramref name="cancellationTokenProvider"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    public EntityTranslationService(
        IEntityTranslationStore store,
        LocalizationOptions options,
        ICancellationTokenProvider cancellationTokenProvider)
    {
        _store = Check.NotNull(store);
        _options = Check.NotNull(options);
        _cancellationTokenProvider = Check.NotNull(cancellationTokenProvider);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyDictionary<EntityTranslationKey, EntityTranslation>> GetFieldsAsync(
        EntityTranslationBatchQuery query,
        CancellationToken cancellationToken = default)
    {
        var token = _cancellationTokenProvider.FallbackToProvider(cancellationToken);
        var candidates = CultureFallback.Expand(query.Context, _options);
        var storeQuery = new EntityTranslationQuery(query.Keys, query.Context, candidates);

        // 仅调用 store 一次
        var translations = await _store.GetListAsync(storeQuery, token).ConfigureAwait(false);

        // 按键建立索引，避免对每个字段/文化重复线性扫描（O(keys×cultures×translations) → 近似 O(translations)）
        var index = new Dictionary<EntityTranslationKey, List<EntityTranslation>>();
        foreach (var t in translations)
        {
            var k = new EntityTranslationKey(t.EntityType, t.EntityId, t.FieldName);
            if (!index.TryGetValue(k, out var bucket)) { bucket = []; index[k] = bucket; }
            bucket.Add(t);
        }

        var result = new Dictionary<EntityTranslationKey, EntityTranslation>();

        foreach (var key in query.Keys)
        {
            var best = SelectBest(key, index, candidates, query.Context.TenantId);
            if (best is not null)
            {
                result[key] = best;
            }
        }

        return result.AsReadOnly();
    }

    /// <inheritdoc />
    public async ValueTask<string?> GetFieldAsync(
        EntityTranslationLookup lookup,
        CancellationToken cancellationToken = default)
    {
        var batchQuery = new EntityTranslationBatchQuery([lookup.Key], lookup.Context);
        var result = await GetFieldsAsync(batchQuery, cancellationToken).ConfigureAwait(false);
        return result.TryGetValue(lookup.Key, out var translation) ? translation.Value : null;
    }

    /// <summary>
    /// 按候选文化顺序（高优先级在前）、当前租户优先于全局租户，从预建索引中选出最佳翻译记录
    /// </summary>
    private static EntityTranslation? SelectBest(
        EntityTranslationKey key,
        Dictionary<EntityTranslationKey, List<EntityTranslation>> index,
        IReadOnlyList<string> candidates,
        string? currentTenantId)
    {
        if (!index.TryGetValue(key, out var bucket))
        {
            return null;
        }

        foreach (var culture in candidates)
        {
            // 当前租户优先
            EntityTranslation? tenantMatch = null;
            EntityTranslation? globalMatch = null;

            foreach (var t in bucket)
            {
                if (!string.Equals(t.CultureName, culture, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (t.TenantId == currentTenantId)
                {
                    tenantMatch = t;
                }
                else if (t.TenantId is null)
                {
                    globalMatch = t;
                }
            }

            if (tenantMatch is not null) { return tenantMatch; }
            if (globalMatch is not null) { return globalMatch; }
        }

        return null;
    }
}
