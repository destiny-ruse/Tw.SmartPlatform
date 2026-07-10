namespace Tw.Observability;

/// <summary>
/// 封装Correlation上下文相关的数据和行为
/// </summary>
public sealed record CorrelationContext(string? TraceId, string? CorrelationId, string? TenantId, string? ShardId);
