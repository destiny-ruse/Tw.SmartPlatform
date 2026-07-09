namespace Tw.Idempotency;

public sealed record IdempotencyKey(
    IdempotencyBoundary Boundary,
    string TenantId,
    string ResourceType,
    string Operation,
    string BusinessKey);
