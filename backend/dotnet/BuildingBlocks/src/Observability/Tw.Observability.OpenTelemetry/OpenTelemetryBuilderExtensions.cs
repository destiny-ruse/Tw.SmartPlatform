using Microsoft.Extensions.DependencyInjection;

namespace Tw.Observability.OpenTelemetry;

public static class OpenTelemetryBuilderExtensions
{
    public static IServiceCollection AddTwOpenTelemetry(this IServiceCollection services, OpenTelemetryRegistrationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = options ?? OpenTelemetryRegistrationOptions.Default;
        return services;
    }
}
