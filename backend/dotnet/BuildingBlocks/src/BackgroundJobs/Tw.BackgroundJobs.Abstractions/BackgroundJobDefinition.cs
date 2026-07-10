namespace Tw.BackgroundJobs.Abstractions;

/// <summary>
/// 封装后台作业定义相关的数据和行为
/// </summary>
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
