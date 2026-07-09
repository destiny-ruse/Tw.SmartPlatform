namespace Tw.Observability;

/// <summary>表示 CorrelationContext 声明</summary>
public sealed record CorrelationContext(string? TraceId, string? CorrelationId, string? TenantId, string? ShardId);
