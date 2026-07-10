namespace Tw.AspNetCore.Abstractions;

/// <summary>
/// 封装请求Correlation相关的数据和行为
/// </summary>
public sealed record RequestCorrelation(string? TraceId, string? CorrelationId);
