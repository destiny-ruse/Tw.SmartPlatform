using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw;

namespace Tw.Context;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供取消令牌上下文能力注册扩展
/// </summary>
public static class CancellationTokenServiceCollectionExtensions
{
    /// <summary>
    /// 注册取消令牌上下文能力
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>
    /// 注册 <see cref="AsyncLocalCancellationTokenScopeProvider"/> 为 singleton，
    /// 并将 <see cref="ICancellationTokenProvider"/> 默认注册为 <see cref="NullCancellationTokenProvider"/>。
    /// 已存在的同类型注册不会被覆盖。
    /// </remarks>
    public static IServiceCollection AddCancellationTokenProvider(this IServiceCollection services)
    {
        Check.NotNull(services);

        services.TryAddSingleton<AsyncLocalCancellationTokenScopeProvider>();
        services.TryAddSingleton<ICancellationTokenProvider, NullCancellationTokenProvider>();

        return services;
    }
}
