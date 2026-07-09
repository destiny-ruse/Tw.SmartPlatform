namespace Tw.BackgroundJobs;

/// <summary>表示 BackgroundJobAuditEvent 声明</summary>
public sealed record BackgroundJobAuditEvent(string TenantId, string ShardId, string JobId, DateTimeOffset StartedAt);

/// <summary>表示 BackgroundJobTraceEvent 声明</summary>
public sealed record BackgroundJobTraceEvent(string TenantId, string ShardId, string JobId, string EventName, DateTimeOffset OccurredAt);

/// <summary>表示 BackgroundJobMetricEvent 声明</summary>
public sealed record BackgroundJobMetricEvent(string TenantId, string ShardId, string JobId, string MetricName, double Value);
