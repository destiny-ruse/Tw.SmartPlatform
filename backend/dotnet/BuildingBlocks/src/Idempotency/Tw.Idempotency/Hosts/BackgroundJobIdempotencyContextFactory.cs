namespace Tw.Idempotency.Hosts;

/// <summary>表示 BackgroundJobIdempotencyContextFactory 类型</summary>
public static class BackgroundJobIdempotencyContextFactory
{
    /// <summary>执行 Create 操作</summary>
    /// <param name="tenantId">tenantId 参数</param>
    /// <param name="jobName">jobName 参数</param>
    /// <param name="fireId">fireId 参数</param>
    /// <returns>Create 的执行结果</returns>
    public static IdempotencyKey Create(string tenantId, string jobName, string fireId)
    {
        HttpIdempotencyContextFactory.Validate(tenantId, jobName, fireId);
        return new IdempotencyKey(IdempotencyBoundary.BackgroundJob, tenantId, jobName, "Execute", fireId);
    }
}
