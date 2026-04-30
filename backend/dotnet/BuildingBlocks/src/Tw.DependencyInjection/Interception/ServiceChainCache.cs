using Tw.Core;
using Tw.DependencyInjection.Registration;

namespace Tw.DependencyInjection.Interception;

/// <summary>
/// 按服务实现类型预计算的 Service 作用域拦截器链缓存
/// </summary>
public sealed class ServiceChainCache
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyList<InterceptorRegistration>> _chains;

    /// <summary>
    /// 使用最终服务注册计划、全局拦截器和自动匹配规则初始化缓存
    /// </summary>
    /// <param name="registrations">最终服务注册描述符集合</param>
    /// <param name="globalInterceptors">全局拦截器注册集合</param>
    /// <param name="matchers">启动期自动匹配规则集合</param>
    public ServiceChainCache(
        IEnumerable<ServiceRegistrationDescriptor> registrations,
        IEnumerable<InterceptorRegistration> globalInterceptors,
        IEnumerable<IInterceptorMatcher> matchers)
    {
        var registrationList = Check.NotNull(registrations).ToList();
        var serviceGlobals = Check.NotNull(globalInterceptors)
            .Where(r => r.Scope == InterceptorScope.Service)
            .ToArray();
        var serviceMatchers = Check.NotNull(matchers)
            .Where(m => m.Scope == InterceptorScope.Service)
            .ToArray();

        _chains = registrationList
            .GroupBy(r => r.ImplementationType)
            .ToDictionary(
                g => g.Key,
                g => BuildChain(g.Key, g, serviceGlobals, serviceMatchers));
    }

    /// <summary>
    /// 获取指定实现类型的 Service 拦截器链
    /// </summary>
    /// <param name="implementationType">服务实现类型</param>
    public IReadOnlyList<InterceptorRegistration> GetInterceptors(Type implementationType)
    {
        Check.NotNull(implementationType);
        return _chains.TryGetValue(implementationType, out var chain)
            ? chain
            : InterceptorMetadata.Empty;
    }

    private static IReadOnlyList<InterceptorRegistration> BuildChain(
        Type implementationType,
        IEnumerable<ServiceRegistrationDescriptor> registrations,
        IReadOnlyList<InterceptorRegistration> globalInterceptors,
        IReadOnlyList<IInterceptorMatcher> matchers)
    {
        var serviceTypes = registrations
            .Select(r => r.ServiceType)
            .Distinct()
            .ToArray();
        var ignoreRules = InterceptorMetadata.ReadIgnoreRules(implementationType, serviceTypes);

        var globalLayer = InterceptorMetadata.SortLayer(
            InterceptorMetadata.ApplyIgnoreRules(globalInterceptors, ignoreRules));

        var matcherLayer = InterceptorMetadata.SortLayer(
            InterceptorMetadata.ApplyIgnoreRules(
                matchers
                    .Where(m => serviceTypes.Any(s => m.MatchService(s, implementationType)))
                    .Select(m => new InterceptorRegistration(m.InterceptorType, InterceptorScope.Service, m.Order)),
                ignoreRules));

        var explicitLayer = InterceptorMetadata.ReadExplicitServiceInterceptors(implementationType, serviceTypes);

        return InterceptorMetadata.ToReadOnlyList(
            globalLayer
                .Concat(matcherLayer)
                .Concat(explicitLayer));
    }
}
