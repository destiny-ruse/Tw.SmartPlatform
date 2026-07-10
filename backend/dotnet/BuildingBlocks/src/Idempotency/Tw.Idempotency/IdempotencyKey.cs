namespace Tw.Idempotency;

/// <summary>
/// 封装幂等请求的边界、资源和业务键
/// </summary>
public sealed record IdempotencyKey(
    IdempotencyBoundary Boundary,
    string TenantId,
    string ResourceType,
    string Operation,
    string BusinessKey);
