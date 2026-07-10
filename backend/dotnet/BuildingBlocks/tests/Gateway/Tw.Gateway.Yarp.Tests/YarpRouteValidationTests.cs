using AwesomeAssertions;
using Tw.Gateway;
using Tw.Gateway.Yarp;
using Xunit;

namespace Tw.Gateway.Yarp.Tests;

/// <summary>
/// 覆盖YarpRouteValidation的核心行为和边界条件
/// </summary>
public sealed class YarpRouteValidationTests
{
    /// <summary>
    /// 验证校验拒绝StrictGlobalLimit带有GatewayLocalLimit
    /// </summary>
    [Fact]
    public void Validate_RejectsStrictGlobalLimitWithGatewayLocalLimit()
    {
        var route = new GatewayRoute(
            "orders",
            "orders",
            "/orders/{**catch-all}",
            ["GET"],
            "http://orders",
            "orders-api",
            100,
            TimeSpan.FromSeconds(3),
            "standard",
            new GatewayRateLimitPolicy(StrictGlobalLimit: true, GatewayLocalLimit: true),
            WebSocketPassThrough: false,
            SsePassThrough: false,
            GrpcPassThrough: false,
            TrustedTenantSource: "jwt");

        var act = () => YarpRouteValidation.Validate(route);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Gateway-local rate limiting cannot be combined with strict global rate limiting.");
    }
}
