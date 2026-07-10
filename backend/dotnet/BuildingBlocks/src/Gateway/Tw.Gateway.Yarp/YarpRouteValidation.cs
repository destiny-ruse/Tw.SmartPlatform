using Tw.Gateway;

namespace Tw.Gateway.Yarp;

/// <summary>
/// 封装YarpRouteValidation相关的数据和行为
/// </summary>
public static class YarpRouteValidation
{
    /// <summary>
    /// 校验当前配置或输入约束，并在非法时抛出异常
    /// </summary>
    /// <param name="route">用于提供route</param>
    public static void Validate(GatewayRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        if (route.RateLimitPolicy.StrictGlobalLimit && route.RateLimitPolicy.GatewayLocalLimit)
        {
            throw new InvalidOperationException("Gateway-local rate limiting cannot be combined with strict global rate limiting.");
        }
    }
}
