using Tw.Auditing.Contracts;

namespace Tw.Auditing;

public sealed class AuditCollector(IAuditStore auditStore)
{
    public Task CollectAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        var redacted = auditEvent with { Details = AuditRedactionPolicy.Redact(auditEvent.Details) };
        return auditStore.StoreAsync(redacted, cancellationToken);
    }
}
