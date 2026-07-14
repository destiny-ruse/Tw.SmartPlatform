using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Tw.AspNetCore.Health;
using Xunit;

namespace Tw.AspNetCore.Tests.Health;

/// <summary>
/// 固定健康检查端点的路由契约
/// </summary>
public sealed class HealthEndpointRouteBuilderExtensionsTests
{
    /// <summary>
    /// 健康检查入口只映射单一 health 路由并返回原端点构建器
    /// </summary>
    [Fact]
    public void MapHealthEndpoint_MapsOnlyHealthRouteAndReturnsSameBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        var application = builder.Build();

        var result = application.MapHealthEndpoint();

        result.Should().BeSameAs(application);
        var routes = ((IEndpointRouteBuilder)application).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToArray();
        routes.Should().Equal("/health");
    }
}
