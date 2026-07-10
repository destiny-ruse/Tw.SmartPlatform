namespace Tw.AspNetCore.Mvc.Responses;

/// <summary>
/// 封装Api响应相关的数据和行为
/// </summary>
/// <typeparam name="T">响应数据的运行时类型</typeparam>
public sealed record ApiResponse<T>(
    bool Success,
    string Code,
    string Message,
    T? Data,
    string? TraceId,
    string? CorrelationId,
    DateTimeOffset Timestamp);
