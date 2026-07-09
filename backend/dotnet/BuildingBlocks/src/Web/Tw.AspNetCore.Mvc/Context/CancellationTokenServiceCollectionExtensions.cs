using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw;
using Tw.Context;

namespace Tw.AspNetCore.Mvc.Context;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 MVC/Razor Pages 请求取消令牌能力注册扩展
/// </summary>
public static class CancellationTokenServiceCollectionExtensions
{
    /// <summary>
    /// 注册 MVC/Razor Pages 请求取消令牌能力
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>
    /// 先调用 <see cref="Tw.Context.CancellationTokenServiceCollectionExtensions.AddCancellationTokenProvider"/> 注册核心能力，
    /// 注册 <c>IHttpContextAccessor</c>，并将 <see cref="ICancellationTokenProvider"/>
    /// 替换为 <see cref="HttpContextCancellationTokenProvider"/>。
    /// </remarks>
    public static IServiceCollection AddHttpContextCancellationTokenProvider(this IServiceCollection services)
    {
        Check.NotNull(services);

        services.AddCancellationTokenProvider();
        services.AddHttpContextAccessor();
        services.Replace(
            ServiceDescriptor.Singleton<ICancellationTokenProvider, HttpContextCancellationTokenProvider>());

        return services;
    }
}
