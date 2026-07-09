using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Tw.AspNetCore.RateLimiting;

/// <summary>表示 RateLimitServiceCollectionExtensions 类型</summary>
public static class RateLimitServiceCollectionExtensions
{
    /// <summary>执行 AddApplicationRateLimiting 操作</summary>
    /// <param name="services">services 参数</param>
    /// <param name="configure">configure 参数</param>
    /// <returns>AddApplicationRateLimiting 的执行结果</returns>
    public static IServiceCollection AddApplicationRateLimiting(
        this IServiceCollection services,
        Action<RateLimiterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddRateLimiter(options => configure?.Invoke(options));
        return services;
    }
}
