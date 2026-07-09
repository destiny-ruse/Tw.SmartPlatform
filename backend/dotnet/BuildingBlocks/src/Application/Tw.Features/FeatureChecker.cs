namespace Tw.Features;

/// <summary>
/// 按 tenant、service、definition default 顺序解析 Feature 值的检查器
/// </summary>
public sealed class FeatureChecker : IFeatureChecker
{
    /// <summary>表示 _store 字段</summary>
    private readonly IFeatureStore _store;
    /// <summary>表示 _cache 字段</summary>
    private readonly IFeatureCache _cache;
    /// <summary>表示 _definitions 字段</summary>
    private readonly IReadOnlyDictionary<string, FeatureDefinition> _definitions;

    /// <summary>
    /// 初始化 Feature 检查器
    /// </summary>
    /// <param name="store">Feature 值存储读取边界</param>
    /// <param name="cache">Feature 值缓存边界</param>
    /// <param name="definitions">Feature 定义集合</param>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> 或 <paramref name="cache"/> 为 null 时抛出</exception>
    public FeatureChecker(
        IFeatureStore store,
        IFeatureCache cache,
        IEnumerable<FeatureDefinition>? definitions = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _definitions = (definitions ?? Array.Empty<FeatureDefinition>())
            .ToDictionary(definition => definition.Name, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public async Task<FeatureCheckResult> CheckAsync(
        string feature,
        string tenantId,
        string serviceName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feature);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var value = await FindValueAsync(feature, FeatureScope.Tenant, tenantId, cancellationToken)
            ?? await FindValueAsync(feature, FeatureScope.Service, serviceName, cancellationToken);

        var enabled = value?.Enabled
            ?? (_definitions.TryGetValue(feature, out var definition) && definition.DefaultEnabled);

        return enabled
            ? FeatureCheckResult.EnabledResult()
            : FeatureCheckResult.Disabled(feature);
    }

    /// <inheritdoc />
    public Task RefreshAsync(FeatureRefreshRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _cache.RemoveAsync(
            new FeatureCacheKey(request.Name, request.Scope, request.ScopeKey),
            cancellationToken);
    }

    /// <summary>执行 FindValueAsync 操作</summary>
    /// <param name="name">name 参数</param>
    /// <param name="scope">scope 参数</param>
    /// <param name="scopeKey">scopeKey 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>FindValueAsync 的执行结果</returns>
    private async Task<FeatureValue?> FindValueAsync(
        string name,
        FeatureScope scope,
        string scopeKey,
        CancellationToken cancellationToken)
    {
        var key = new FeatureCacheKey(name, scope, scopeKey);
        var cached = await _cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await _store.FindAsync(name, scope, scopeKey, cancellationToken);
        if (value is not null)
        {
            await _cache.SetAsync(key, value, cancellationToken);
        }

        return value;
    }
}
