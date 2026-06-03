using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw;
using Tw.Context;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 <c>Tw.Core</c> 核心能力注册扩展
/// </summary>
public static class TwCoreServiceCollectionExtensions
{
    /// <summary>
    /// 注册 <c>Tw.Core</c> 取消令牌上下文能力
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>
    /// 注册 <see cref="AsyncLocalCancellationTokenScopeProvider"/> 为 singleton，
    /// 并将 <see cref="ICancellationTokenProvider"/> 默认注册为 <see cref="NullCancellationTokenProvider"/>。
    /// 已存在的同类型注册不会被覆盖。
    /// </remarks>
    public static IServiceCollection AddTwCore(this IServiceCollection services)
    {
        Check.NotNull(services);

        services.TryAddSingleton<AsyncLocalCancellationTokenScopeProvider>();
        services.TryAddSingleton<ICancellationTokenProvider, NullCancellationTokenProvider>();

        return services;
    }
}
