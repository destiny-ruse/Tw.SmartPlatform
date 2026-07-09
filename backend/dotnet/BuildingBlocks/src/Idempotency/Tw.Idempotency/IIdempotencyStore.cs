namespace Tw.Idempotency;

/// <summary>定义 IIdempotencyStore 契约</summary>
public interface IIdempotencyStore
{
    /// <summary>执行 TryBeginAsync 操作</summary>
    /// <param name="key">key 参数</param>
    /// <param name="fingerprint">fingerprint 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>TryBeginAsync 的执行结果</returns>
    Task<IdempotencyReservation> TryBeginAsync(IdempotencyKey key, string fingerprint, CancellationToken cancellationToken = default);

    /// <summary>执行 GetAsync 操作</summary>
    /// <param name="key">key 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>GetAsync 的执行结果</returns>
    Task<IdempotencyResult?> GetAsync(IdempotencyKey key, CancellationToken cancellationToken = default);

    /// <summary>执行 CompleteAsync 操作</summary>
    /// <param name="key">key 参数</param>
    /// <param name="result">result 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>CompleteAsync 的执行结果</returns>
    Task CompleteAsync(IdempotencyKey key, IdempotencyResult result, CancellationToken cancellationToken = default);
}
