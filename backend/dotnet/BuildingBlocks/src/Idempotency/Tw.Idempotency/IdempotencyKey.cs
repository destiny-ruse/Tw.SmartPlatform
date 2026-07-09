namespace Tw.Idempotency;

/// <summary>表示 IdempotencyKey 声明</summary>
public sealed record IdempotencyKey(
    IdempotencyBoundary Boundary,
    string TenantId,
    string ResourceType,
    string Operation,
    string BusinessKey);
