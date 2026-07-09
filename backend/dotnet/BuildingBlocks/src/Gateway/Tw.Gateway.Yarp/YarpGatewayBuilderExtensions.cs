using Microsoft.Extensions.DependencyInjection;

namespace Tw.Gateway.Yarp;

public static class YarpGatewayBuilderExtensions
{
    public static IServiceCollection AddTwYarpGateway(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
