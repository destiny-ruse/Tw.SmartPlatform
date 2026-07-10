namespace Tw.Auditing.Contracts;

/// <summary>
/// 封装审计Actor相关的数据和行为
/// </summary>
public sealed record AuditActor(string ActorId, string TenantId, string Source);
