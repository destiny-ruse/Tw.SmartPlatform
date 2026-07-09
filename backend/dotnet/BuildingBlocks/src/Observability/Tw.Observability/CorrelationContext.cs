namespace Tw.Observability;

public sealed record CorrelationContext(string? TraceId, string? CorrelationId, string? TenantId, string? ShardId);
