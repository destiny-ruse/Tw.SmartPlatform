namespace Tw.Idempotency;

/// <summary>表示 IdempotencyConflictException 类型</summary>
public sealed class IdempotencyConflictException(IdempotencyKey key)
    : Exception("Idempotency key has already been used with different request content.")
{
    /// <summary>表示 Code 属性</summary>
    public string Code { get; } = "IDEMPOTENCY:000409";

    /// <summary>表示 Key 属性</summary>
    public IdempotencyKey Key { get; } = key;
}
