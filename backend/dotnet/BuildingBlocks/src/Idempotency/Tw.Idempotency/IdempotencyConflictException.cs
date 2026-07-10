namespace Tw.Idempotency;

/// <summary>
/// 描述幂等键已被不同请求内容使用的冲突异常
/// </summary>
public sealed class IdempotencyConflictException(IdempotencyKey key)
    : Exception("Idempotency key has already been used with different request content.")
{
    /// <summary>
    /// 代码在当前对象中的业务含义
    /// </summary>
    public string Code { get; } = "IDEMPOTENCY:000409";

    /// <summary>
    /// 键在当前对象中的业务含义
    /// </summary>
    public IdempotencyKey Key { get; } = key;
}
