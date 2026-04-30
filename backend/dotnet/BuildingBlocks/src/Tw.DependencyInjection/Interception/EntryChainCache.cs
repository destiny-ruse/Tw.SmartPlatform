using System.Collections.Concurrent;
using Tw.Core;

namespace Tw.DependencyInjection.Interception;

/// <summary>
/// 按入口类型懒计算的 Entry 作用域拦截器链缓存
/// </summary>
public sealed class EntryChainCache
{
    private readonly IReadOnlyList<InterceptorRegistration> _globalInterceptors;
    private readonly IReadOnlyList<IInterceptorMatcher> _matchers;
    private readonly ConcurrentDictionary<Type, IReadOnlyList<InterceptorRegistration>> _chains = new();

    /// <summary>
    /// 使用全局拦截器和自动匹配规则初始化缓存
    /// </summary>
    /// <param name="globalInterceptors">全局拦截器注册集合</param>
    /// <param name="matchers">启动期自动匹配规则集合</param>
    public EntryChainCache(
        IEnumerable<InterceptorRegistration> globalInterceptors,
        IEnumerable<IInterceptorMatcher> matchers)
    {
        _globalInterceptors = Check.NotNull(globalInterceptors)
            .Where(r => r.Scope == InterceptorScope.Entry)
            .ToArray();
        _matchers = Check.NotNull(matchers)
            .Where(m => m.Scope == InterceptorScope.Entry)
            .ToArray();
    }

    /// <summary>
    /// 获取指定入口类型的 Entry 拦截器链
    /// </summary>
    /// <param name="entryType">MVC Controller、gRPC Service 或其他入口类型</param>
    public IReadOnlyList<InterceptorRegistration> GetInterceptors(Type entryType)
    {
        Check.NotNull(entryType);
        return _chains.GetOrAdd(entryType, BuildChain);
    }

    private IReadOnlyList<InterceptorRegistration> BuildChain(Type entryType)
    {
        var ignoreRules = InterceptorMetadata.ReadIgnoreRules(entryType, []);

        var globalLayer = InterceptorMetadata.SortLayer(
            InterceptorMetadata.ApplyIgnoreRules(_globalInterceptors, ignoreRules));

        var matcherLayer = InterceptorMetadata.SortLayer(
            InterceptorMetadata.ApplyIgnoreRules(
                _matchers
                    .Where(m => m.MatchEntry(entryType))
                    .Select(m => new InterceptorRegistration(m.InterceptorType, InterceptorScope.Entry, m.Order)),
                ignoreRules));

        return InterceptorMetadata.ToReadOnlyList(globalLayer.Concat(matcherLayer));
    }
}
