namespace Tw.BackgroundJobs.Abstractions;

public sealed record BackgroundJobContext(string TenantId, string ShardId, string JobId, DateTimeOffset StartedAt);
