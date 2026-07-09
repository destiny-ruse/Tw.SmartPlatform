namespace Tw.BackgroundJobs.Abstractions;

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
