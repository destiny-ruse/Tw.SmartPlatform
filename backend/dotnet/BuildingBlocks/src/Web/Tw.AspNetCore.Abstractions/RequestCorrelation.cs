namespace Tw.AspNetCore.Abstractions;

public sealed record RequestCorrelation(string? TraceId, string? CorrelationId);
