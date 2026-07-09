using Tw.Auditing.Contracts;

namespace Tw.Auditing;

/// <summary>表示 AuditCollector 类型</summary>
public sealed class AuditCollector(IAuditStore auditStore)
{
    /// <summary>执行 CollectAsync 操作</summary>
    /// <param name="auditEvent">auditEvent 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>CollectAsync 的执行结果</returns>
    public Task CollectAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        var redacted = auditEvent with { Details = AuditRedactionPolicy.Redact(auditEvent.Details) };
        return auditStore.StoreAsync(redacted, cancellationToken);
    }
}
