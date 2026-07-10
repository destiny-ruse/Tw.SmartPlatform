using Tw.Auditing.Contracts;

namespace Tw.Auditing;

/// <summary>
/// 封装审计Collector相关的数据和行为
/// </summary>
public sealed class AuditCollector(IAuditStore auditStore)
{
    /// <summary>
    /// 说明CollectAsync在当前类型中的职责
    /// </summary>
    /// <param name="auditEvent">用于提供auditEvent</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    public Task CollectAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        var redacted = auditEvent with { Details = AuditRedactionPolicy.Redact(auditEvent.Details) };
        return auditStore.StoreAsync(redacted, cancellationToken);
    }
}
