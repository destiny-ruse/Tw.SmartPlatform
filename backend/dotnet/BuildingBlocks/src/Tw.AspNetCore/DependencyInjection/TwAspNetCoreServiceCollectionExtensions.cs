using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw;
using Tw.AspNetCore.Context;
using Tw.Context;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 <c>Tw.AspNetCore</c> 宿主集成注册扩展
/// </summary>
public static class TwAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// 注册 <c>Tw.AspNetCore</c> 宿主取消令牌能力
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>
    /// 先调用 <see cref="TwCoreServiceCollectionExtensions.AddTwCore"/> 注册核心能力，
    /// 注册 <c>IHttpContextAccessor</c>，并将 <see cref="ICancellationTokenProvider"/>
    /// 替换为 <see cref="HttpContextCancellationTokenProvider"/>。
    /// </remarks>
    public static IServiceCollection AddTwAspNetCore(this IServiceCollection services)
    {
        Check.NotNull(services);

        services.AddTwCore();
        services.AddHttpContextAccessor();
        services.Replace(
            ServiceDescriptor.Singleton<ICancellationTokenProvider, HttpContextCancellationTokenProvider>());

        return services;
    }
}
