using Tw.Gateway;

namespace Tw.Gateway.Yarp;

/// <summary>表示 YarpRouteValidation 类型</summary>
public static class YarpRouteValidation
{
    /// <summary>执行 Validate 操作</summary>
    /// <param name="route">route 参数</param>
    public static void Validate(GatewayRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        if (route.RateLimitPolicy.StrictGlobalLimit && route.RateLimitPolicy.GatewayLocalLimit)
        {
            throw new InvalidOperationException("Gateway-local rate limiting cannot be combined with strict global rate limiting.");
        }
    }
}
