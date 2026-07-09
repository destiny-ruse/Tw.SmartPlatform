using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Tw.AspNetCore.Middleware;

/// <summary>表示 ExceptionHandlingMiddleware 类型</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    /// <summary>执行 InvokeAsync 操作</summary>
    /// <param name="context">context 参数</param>
    /// <returns>InvokeAsync 的执行结果</returns>
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

    /// <summary>表示 ErrorResponse 声明</summary>
    private sealed record ErrorResponse(
        bool Success,
        string Code,
        string Message,
        string? TraceId,
        string? CorrelationId,
        DateTimeOffset Timestamp);
}
