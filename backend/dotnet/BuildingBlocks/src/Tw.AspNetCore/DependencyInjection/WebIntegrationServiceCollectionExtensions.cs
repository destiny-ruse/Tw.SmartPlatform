using Microsoft.Extensions.DependencyInjection;
using Tw;
using Tw.AspNetCore.Context;

namespace Tw.AspNetCore;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 <c>Tw.AspNetCore</c> Web 集成聚合注册入口
/// </summary>
/// <remarks>
/// 聚合入口按固定顺序调用本程序集内的功能级注册方法，使业务应用无需了解功能注册顺序。
/// 聚合入口不替代功能级注册方法；功能级注册方法仍可单独调用和组合。
/// </remarks>
public static class WebIntegrationServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Web 集成所需的全部功能能力
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>当前聚合 HTTP 请求取消令牌能力；后续 Web 功能在此追加。</remarks>
    public static IServiceCollection AddWebIntegration(this IServiceCollection services)
    {
        Check.NotNull(services);

        services.AddHttpContextCancellationTokenProvider();

        return services;
    }
}
