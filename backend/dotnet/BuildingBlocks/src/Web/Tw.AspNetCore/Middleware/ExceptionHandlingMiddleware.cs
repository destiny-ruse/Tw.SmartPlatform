using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Tw.AspNetCore.Middleware;

/// <summary>
/// 封装异常HandlingMiddleware相关的数据和行为
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    /// <summary>
    /// 执行测试管道委托并记录调用
    /// </summary>
    /// <param name="context">当前调用携带的上下文信息</param>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 封装错误响应相关的数据和行为
    /// </summary>
    private sealed record ErrorResponse(
        bool Success,
        string Code,
        string Message,
        string? TraceId,
        string? CorrelationId,
        DateTimeOffset Timestamp);
}
