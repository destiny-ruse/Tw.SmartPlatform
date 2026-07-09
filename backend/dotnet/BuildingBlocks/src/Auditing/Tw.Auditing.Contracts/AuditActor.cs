namespace Tw.Auditing.Contracts;

public sealed record AuditActor(string ActorId, string TenantId, string Source);
