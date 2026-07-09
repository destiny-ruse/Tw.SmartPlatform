namespace Tw.AspNetCore.Mvc.Responses;

/// <summary>表示 ApiResponse 声明</summary>
/// <typeparam name="T">T 类型参数</typeparam>
public sealed record ApiResponse<T>(
    bool Success,
    string Code,
    string Message,
    T? Data,
    string? TraceId,
    string? CorrelationId,
    DateTimeOffset Timestamp);
