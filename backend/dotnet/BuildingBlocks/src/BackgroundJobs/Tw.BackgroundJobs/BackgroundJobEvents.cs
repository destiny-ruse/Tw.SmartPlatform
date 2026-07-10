namespace Tw.BackgroundJobs;

/// <summary>
/// 封装后台作业审计事件相关的数据和行为
/// </summary>
public sealed record BackgroundJobAuditEvent(string TenantId, string ShardId, string JobId, DateTimeOffset StartedAt);

/// <summary>
/// 封装后台作业Trace事件相关的数据和行为
/// </summary>
public sealed record BackgroundJobTraceEvent(string TenantId, string ShardId, string JobId, string EventName, DateTimeOffset OccurredAt);

/// <summary>
/// 封装后台作业Metric事件相关的数据和行为
/// </summary>
public sealed record BackgroundJobMetricEvent(string TenantId, string ShardId, string JobId, string MetricName, double Value);
