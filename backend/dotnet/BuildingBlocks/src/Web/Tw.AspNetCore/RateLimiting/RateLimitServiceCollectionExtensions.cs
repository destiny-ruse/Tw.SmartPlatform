using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Tw.AspNetCore.RateLimiting;

public static class RateLimitServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationRateLimiting(
        this IServiceCollection services,
        Action<RateLimiterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddRateLimiter(options => configure?.Invoke(options));
        return services;
    }
}
