namespace Tw.BackgroundJobs.Abstractions;

/// <summary>
/// 封装后台作业上下文相关的数据和行为
/// </summary>
public sealed record BackgroundJobContext(string TenantId, string ShardId, string JobId, DateTimeOffset StartedAt);
