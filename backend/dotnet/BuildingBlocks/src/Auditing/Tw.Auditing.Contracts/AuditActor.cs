namespace Tw.Auditing.Contracts;

/// <summary>表示 AuditActor 声明</summary>
public sealed record AuditActor(string ActorId, string TenantId, string Source);
