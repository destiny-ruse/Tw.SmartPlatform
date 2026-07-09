using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Tw.AspNetCore.Health;

/// <summary>表示 HealthEndpointRouteBuilderExtensions 类型</summary>
public static class HealthEndpointRouteBuilderExtensions
{
    /// <summary>执行 MapTwHealthEndpoints 操作</summary>
    /// <param name="endpoints">endpoints 参数</param>
    /// <returns>MapTwHealthEndpoints 的执行结果</returns>
    public static IEndpointRouteBuilder MapTwHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
        return endpoints;
    }
}
