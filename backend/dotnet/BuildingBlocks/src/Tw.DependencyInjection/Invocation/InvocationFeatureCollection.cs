using Tw.Core;

namespace Tw.DependencyInjection.Invocation;

/// <summary>
/// 类型化特性集合，按类型存取场景特定的运行时数据。
/// 非线程安全；在单次调用链内单线程访问。
/// </summary>
public sealed class InvocationFeatureCollection
{
    private readonly Dictionary<Type, object> _features = new();

    /// <summary>
    /// 以 <typeparamref name="TFeature"/> 为键注册特性实例，已存在时覆盖。
    /// </summary>
    public void Set<TFeature>(TFeature feature) where TFeature : class
    {
        Check.NotNull(feature);
        _features[typeof(TFeature)] = feature;
    }

    /// <summary>
    /// 按 <typeparamref name="TFeature"/> 类型取出特性实例；不存在时返回 <see langword="null"/>。
    /// </summary>
    public TFeature? Get<TFeature>() where TFeature : class
        => _features.TryGetValue(typeof(TFeature), out var value) ? (TFeature?)value : null;
}
