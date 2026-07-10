namespace Tw.Idempotency;

/// <summary>
/// 协调幂等请求的占用、复用、冲突检测和结果保存
/// </summary>
public sealed class IdempotencyExecutor(IIdempotencyStore store)
{
    /// <summary>
    /// 异步执行当前组件的核心处理流程
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="fingerprint">用于区分幂等请求负载的指纹</param>
    /// <param name="operation">需要在幂等保护下运行的业务委托</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的幂等处理结果</returns>
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
