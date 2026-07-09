using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Tw.AspNetCore.Health;

public static class HealthEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTwHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
        return endpoints;
    }
}
