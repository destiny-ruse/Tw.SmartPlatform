namespace Tw.BackgroundJobs.Abstractions;

/// <summary>表示 BackgroundJobDefinition 声明</summary>
public sealed record BackgroundJobDefinition(
    string Name,
    Type ArgumentType,
    string Schedule,
    string TenantBehavior,
    TimeSpan Timeout,
    string RetryPolicyName,
    string AuditCategory,
    bool IsClustered,
    string SchedulerDatabaseKey);
