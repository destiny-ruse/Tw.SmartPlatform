namespace Tw.DependencyInjection.Invocation;

/// <summary>
/// 基础调用上下文，提供取消令牌、可写 Items 字典和类型化特性集合访问。
/// </summary>
public interface IInvocationContext
{
    /// <summary>
    /// 当前调用链关联的取消令牌。
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// 可在拦截器链中读写的任意键值对存储，键采用 Ordinal 比较。
    /// </summary>
    IDictionary<string, object?> Items { get; }

    /// <summary>
    /// 按类型获取已注册的场景特性；不存在时返回 <see langword="null"/>。
    /// </summary>
    TFeature? GetFeature<TFeature>() where TFeature : class;
}
