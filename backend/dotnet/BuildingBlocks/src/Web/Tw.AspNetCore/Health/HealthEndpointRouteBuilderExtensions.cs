using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Tw.AspNetCore.Health;

/// <summary>
/// 提供标准健康检查端点的路由映射入口
/// </summary>
public static class HealthEndpointRouteBuilderExtensions
{
    /// <summary>
    /// 将单一健康检查端点映射到 <c>/health</c>
    /// </summary>
    /// <param name="endpoints">用于注册 HTTP 路由的端点构建器</param>
    /// <returns>调用方传入的同一端点构建器</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoints"/> 为 <see langword="null"/> 时抛出</exception>
    public static IEndpointRouteBuilder MapHealthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
        return endpoints;
    }
}
