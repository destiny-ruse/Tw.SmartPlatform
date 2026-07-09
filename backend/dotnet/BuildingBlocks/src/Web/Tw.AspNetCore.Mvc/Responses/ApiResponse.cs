namespace Tw.AspNetCore.Mvc.Responses;

public sealed record ApiResponse<T>(
    bool Success,
    string Code,
    string Message,
    T? Data,
    string? TraceId,
    string? CorrelationId,
    DateTimeOffset Timestamp);
