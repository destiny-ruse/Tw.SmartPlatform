namespace Tw.DependencyInjection.Invocation;

/// <summary>
/// 基础调用上下文实现，包含 ambient 取消令牌、可写 Items 字典和类型化特性集合。
/// </summary>
public sealed class InvocationContext : IInvocationContext
{
    private readonly InvocationFeatureCollection _features;

    /// <summary>
    /// 初始化 <see cref="InvocationContext"/>。
    /// </summary>
    /// <param name="cancellationToken">绑定到本次调用的取消令牌。</param>
    /// <param name="features">可选的外部特性集合；为 <see langword="null"/> 时创建空集合。</param>
    public InvocationContext(CancellationToken cancellationToken, InvocationFeatureCollection? features = null)
    {
        CancellationToken = cancellationToken;
        Items = new Dictionary<string, object?>(StringComparer.Ordinal);
        _features = features ?? new InvocationFeatureCollection();
    }

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; }

    /// <inheritdoc />
    public IDictionary<string, object?> Items { get; }

    /// <inheritdoc />
    public TFeature? GetFeature<TFeature>() where TFeature : class => _features.Get<TFeature>();

    /// <summary>
    /// 暴露底层特性集合，供 Adapter 在执行前注入场景特性。
    /// </summary>
    public InvocationFeatureCollection Features => _features;
}
