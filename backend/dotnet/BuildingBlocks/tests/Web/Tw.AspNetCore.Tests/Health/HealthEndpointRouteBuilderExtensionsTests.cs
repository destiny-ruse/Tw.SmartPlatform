using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
        builder.Services.AddHealthChecks();
        var application = builder.Build();

        var result = application.MapHealthEndpoint();

        result.Should().BeSameAs(application);
        var routes = HealthRoutes(application)
            .Select(endpoint => endpoint.RoutePattern.RawText);
        routes.Should().Equal("/health");
    }

    /// <summary>
    /// 失败的内置健康检查返回框架默认的不可用状态和响应正文
    /// </summary>
    [Fact]
    public async Task MapHealthEndpoint_WhenHealthCheckFails_ReturnsBuiltInUnhealthyResponse()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services
            .AddHealthChecks()
            .AddCheck("failed-check", () => HealthCheckResult.Unhealthy("依赖不可用"));
        await using var application = builder.Build();
        application.MapHealthEndpoint();
        var endpoint = HealthRoutes(application).Single();
        var context = new DefaultHttpContext
        {
            RequestServices = application.Services,
            Response = { Body = new MemoryStream() }
        };

        await endpoint.RequestDelegate!(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        body.Should().Be("Unhealthy");
    }

    /// <summary>
    /// 同一端点构建器重复调用时只保留一个 health 路由
    /// </summary>
    [Fact]
    public void MapHealthEndpoint_WhenCalledTwice_MapsSingleRoute()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthChecks();
        var application = builder.Build();

        application.MapHealthEndpoint();
        var result = application.MapHealthEndpoint();

        result.Should().BeSameAs(application);
        HealthRoutes(application).Should().ContainSingle();
    }

    /// <summary>
    /// 同一端点构建器并发调用时只保留一个 health 路由
    /// </summary>
    [Fact]
    public async Task MapHealthEndpoint_WhenCalledConcurrently_MapsSingleRoute()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthChecks();
        var application = builder.Build();

        var results = await Task.WhenAll(
            Enumerable.Range(0, 32)
                .Select(_ => Task.Run(() => application.MapHealthEndpoint())));

        results.Should().OnlyContain(result => ReferenceEquals(result, application));
        HealthRoutes(application).Should().ContainSingle();
    }

    /// <summary>
    /// 读取应用中映射到 health 路径的路由端点
    /// </summary>
    /// <param name="application">提供端点数据源的 Web 应用</param>
    /// <returns>路由模板等于 <c>/health</c> 的端点集合</returns>
    private static RouteEndpoint[] HealthRoutes(WebApplication application)
    {
        return ((IEndpointRouteBuilder)application).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText == "/health")
            .ToArray();
    }
}
