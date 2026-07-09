using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Tw.AspNetCore.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse(
                Success: false,
                Code: "SYSTEM:UNHANDLED",
                Message: exception.Message,
                TraceId: context.TraceIdentifier,
                CorrelationId: context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId)
                    ? correlationId.ToString()
                    : null,
                Timestamp: DateTimeOffset.UtcNow);

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    private sealed record ErrorResponse(
        bool Success,
        string Code,
        string Message,
        string? TraceId,
        string? CorrelationId,
        DateTimeOffset Timestamp);
}
