namespace Tw.BackgroundJobs.Abstractions;

/// <summary>表示 BackgroundJobContext 声明</summary>
public sealed record BackgroundJobContext(string TenantId, string ShardId, string JobId, DateTimeOffset StartedAt);
