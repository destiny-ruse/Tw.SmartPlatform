namespace Tw.AspNetCore.Mvc.Responses;

public static class ApiErrorResponseFactory
{
    public static ApiResponse<object> Create(string code, string message, string? traceId = null, string? correlationId = null)
    {
        return new ApiResponse<object>(false, code, message, null, traceId, correlationId, DateTimeOffset.UtcNow);
    }
}
