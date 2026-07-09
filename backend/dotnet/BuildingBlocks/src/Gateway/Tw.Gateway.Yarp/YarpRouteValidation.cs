using Tw.Gateway;

namespace Tw.Gateway.Yarp;

public static class YarpRouteValidation
{
    public static void Validate(GatewayRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        if (route.RateLimitPolicy.StrictGlobalLimit && route.RateLimitPolicy.GatewayLocalLimit)
        {
            throw new InvalidOperationException("Gateway-local rate limiting cannot be combined with strict global rate limiting.");
        }
    }
}
