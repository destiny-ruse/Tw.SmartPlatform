using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Tw.AspNetCore.RateLimiting;

/// <summary>
/// 封装RateLimit服务CollectionExtensions相关的数据和行为
/// </summary>
public static class RateLimitServiceCollectionExtensions
{
    /// <summary>
    /// 注册ApplicationRateLimiting所需服务
    /// </summary>
    /// <param name="services">需要注册组件依赖的服务集合</param>
    /// <param name="configure">用于提供configure</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static IServiceCollection AddApplicationRateLimiting(
        this IServiceCollection services,
        Action<RateLimiterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddRateLimiter(options => configure?.Invoke(options));
        return services;
    }
}
