namespace Tw.Settings;

/// <summary>
/// 按 user、tenant、service、definition default 顺序解析 Setting 值的读取服务
/// </summary>
public sealed class SettingProvider : ISettingProvider
{
    private readonly ISettingStore _store;
    private readonly ISettingCache _cache;
    private readonly IReadOnlyDictionary<string, SettingDefinition> _definitions;

    /// <summary>
    /// 初始化 Setting 读取服务
    /// </summary>
    /// <param name="store">Setting 值存储读取边界</param>
    /// <param name="cache">Setting 值缓存边界</param>
    /// <param name="definitions">Setting 定义集合</param>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> 或 <paramref name="cache"/> 为 null 时抛出</exception>
    public SettingProvider(
        ISettingStore store,
        ISettingCache cache,
        IEnumerable<SettingDefinition>? definitions = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _definitions = (definitions ?? Array.Empty<SettingDefinition>())
            .ToDictionary(definition => definition.Name, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(
        string name,
        string tenantId,
        string serviceName,
        string? userId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var value = !string.IsNullOrWhiteSpace(userId)
            ? await FindValueAsync(name, SettingScope.User, userId, cancellationToken)
            : null;

        value ??= await FindValueAsync(name, SettingScope.Tenant, tenantId, cancellationToken)
            ?? await FindValueAsync(name, SettingScope.Service, serviceName, cancellationToken);

        return value?.Value
            ?? (_definitions.TryGetValue(name, out var definition) ? definition.DefaultValue : null);
    }

    /// <inheritdoc />
    public Task RefreshAsync(SettingRefreshRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _cache.RemoveAsync(
            new SettingCacheKey(request.Name, request.Scope, request.ScopeKey),
            cancellationToken);
    }

    private async Task<SettingValue?> FindValueAsync(
        string name,
        SettingScope scope,
        string scopeKey,
        CancellationToken cancellationToken)
    {
        var key = new SettingCacheKey(name, scope, scopeKey);
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
