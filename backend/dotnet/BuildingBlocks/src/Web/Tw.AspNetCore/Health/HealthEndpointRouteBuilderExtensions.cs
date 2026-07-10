using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Tw.AspNetCore.Health;

/// <summary>
/// 封装HealthEndpointRoute构建器Extensions相关的数据和行为
/// </summary>
public static class HealthEndpointRouteBuilderExtensions
{
    /// <summary>
    /// 将TwHealthEndpoints注册到路由或映射表
    /// </summary>
    /// <param name="endpoints">用于注册 HTTP 路由的端点构建器</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static IEndpointRouteBuilder MapTwHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
        return endpoints;
    }
}
