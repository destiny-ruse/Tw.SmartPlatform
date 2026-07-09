using Microsoft.Extensions.DependencyInjection;

namespace Tw.Resilience;

public static class HttpResilienceServiceCollectionExtensions
{
    public static IServiceCollection AddTwHttpResilience(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
