namespace Tw.Idempotency;

/// <summary>表示 IdempotencyExecutor 类型</summary>
public sealed class IdempotencyExecutor(IIdempotencyStore store)
{
    /// <summary>执行 ExecuteAsync 操作</summary>
    /// <param name="key">key 参数</param>
    /// <param name="fingerprint">fingerprint 参数</param>
    /// <param name="operation">operation 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ExecuteAsync 的执行结果</returns>
    public async Task<IdempotencyResult> ExecuteAsync(
        IdempotencyKey key,
        string fingerprint,
        Func<Task<IdempotencyResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);
        ArgumentNullException.ThrowIfNull(operation);

        var reservation = await store.TryBeginAsync(key, fingerprint, cancellationToken);
        if (reservation.Status == IdempotencyReservationStatus.Duplicate)
        {
            return reservation.ExistingResult
                ?? await store.GetAsync(key, cancellationToken)
                ?? IdempotencyResult.Conflict("IDEMPOTENCY:000409");
        }

        if (reservation.Status == IdempotencyReservationStatus.Conflict)
        {
            throw new IdempotencyConflictException(key);
        }

        var result = await operation();
        await store.CompleteAsync(key, result, cancellationToken);
        return result;
    }
}
