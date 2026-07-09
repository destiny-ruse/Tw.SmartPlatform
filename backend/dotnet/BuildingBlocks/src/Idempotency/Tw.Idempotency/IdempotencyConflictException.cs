namespace Tw.Idempotency;

public sealed class IdempotencyConflictException(IdempotencyKey key)
    : Exception("Idempotency key has already been used with different request content.")
{
    public string Code { get; } = "IDEMPOTENCY:000409";

    public IdempotencyKey Key { get; } = key;
}
