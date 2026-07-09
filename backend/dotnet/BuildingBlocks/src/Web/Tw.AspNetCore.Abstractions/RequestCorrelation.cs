namespace Tw.AspNetCore.Abstractions;

/// <summary>表示 RequestCorrelation 声明</summary>
public sealed record RequestCorrelation(string? TraceId, string? CorrelationId);
