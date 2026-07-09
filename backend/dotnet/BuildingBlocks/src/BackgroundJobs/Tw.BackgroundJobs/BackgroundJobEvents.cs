namespace Tw.BackgroundJobs;

public sealed record BackgroundJobAuditEvent(string TenantId, string ShardId, string JobId, DateTimeOffset StartedAt);

public sealed record BackgroundJobTraceEvent(string TenantId, string ShardId, string JobId, string EventName, DateTimeOffset OccurredAt);

public sealed record BackgroundJobMetricEvent(string TenantId, string ShardId, string JobId, string MetricName, double Value);
