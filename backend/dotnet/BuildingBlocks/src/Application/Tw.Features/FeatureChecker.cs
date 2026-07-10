namespace Tw.Features;

/// <summary>
/// 按 tenant、service、definition default 顺序解析 Feature 值的检查器
/// </summary>
public sealed class FeatureChecker : IFeatureChecker
{
    /// <summary>
    /// 保存当前类型处理流程依赖的存储
    /// </summary>
    private readonly IFeatureStore _store;
    /// <summary>
    /// 保存当前类型处理流程依赖的缓存
    /// </summary>
    private readonly IFeatureCache _cache;
    /// <summary>
    /// 保存当前类型处理流程依赖的definitions
    /// </summary>
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

    /// <summary>
    /// 异步查找值并在不存在时返回空值
    /// </summary>
    /// <param name="name">待匹配成员或资源的名称</param>
    /// <param name="scope">功能值生效的作用域</param>
    /// <param name="scopeKey">作用域内定位主体或租户的键</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的功能值</returns>
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
