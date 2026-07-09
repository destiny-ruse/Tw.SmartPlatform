namespace Tw.Auditing.Contracts;

public interface IAuditStore
{
    Task StoreAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
