namespace Tw.Auditing.Contracts;

/// <summary>
/// 定义审计存储的能力边界
/// </summary>
public interface IAuditStore
{
    /// <summary>
    /// 说明存储Async在当前类型中的职责
    /// </summary>
    /// <param name="auditEvent">用于提供auditEvent</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task StoreAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
