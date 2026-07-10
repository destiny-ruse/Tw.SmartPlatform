namespace Tw.Idempotency.Hosts;

/// <summary>
/// 根据后台作业触发信息创建幂等键上下文
/// </summary>
public static class BackgroundJobIdempotencyContextFactory
{
    /// <summary>
    /// 创建统一 API 错误响应对象
    /// </summary>
    /// <param name="tenantId">用于提供tenant标识</param>
    /// <param name="jobName">需要变更状态的后台作业名称</param>
    /// <param name="fireId">用于提供fire标识</param>
    /// <returns>方法计算得到的文本值</returns>
    public static IdempotencyKey Create(string tenantId, string jobName, string fireId)
    {
        HttpIdempotencyContextFactory.Validate(tenantId, jobName, fireId);
        return new IdempotencyKey(IdempotencyBoundary.BackgroundJob, tenantId, jobName, "Execute", fireId);
    }
}
