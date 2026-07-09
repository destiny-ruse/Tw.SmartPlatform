namespace Tw.AspNetCore.Mvc.Responses;

/// <summary>表示 ApiErrorResponseFactory 类型</summary>
public static class ApiErrorResponseFactory
{
    /// <summary>执行 Create 操作</summary>
    /// <param name="code">code 参数</param>
    /// <param name="message">message 参数</param>
    /// <param name="traceId">traceId 参数</param>
    /// <param name="correlationId">correlationId 参数</param>
    /// <returns>Create 的执行结果</returns>
    public static ApiResponse<object> Create(string code, string message, string? traceId = null, string? correlationId = null)
    {
        return new ApiResponse<object>(false, code, message, null, traceId, correlationId, DateTimeOffset.UtcNow);
    }
}
