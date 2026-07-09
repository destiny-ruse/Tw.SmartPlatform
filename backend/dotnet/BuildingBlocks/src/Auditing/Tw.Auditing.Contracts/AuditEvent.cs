namespace Tw.Auditing.Contracts;

public sealed record AuditEvent(
    AuditActor Actor,
    AuditAction Action,
    string Resource,
    string Result,
    string? ErrorCode,
    string? Details,
    string? CorrelationId,
    string? TraceId,
    DateTimeOffset Timestamp)
{
    public static AuditEvent SecurityDenied(AuditActor actor, string actionName, string errorCode)
    {
        return new AuditEvent(actor, new AuditAction(actionName), string.Empty, "Denied", errorCode, null, null, null, DateTimeOffset.UtcNow);
    }

    public static AuditEvent ConfigurationChanged(AuditActor actor, string key, string oldValue, string newValue)
    {
        return new AuditEvent(actor, new AuditAction("Configuration.Changed"), key, "Succeeded", null, $"old={oldValue};new={newValue}", null, null, DateTimeOffset.UtcNow);
    }
}
