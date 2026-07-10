namespace Tw.Idempotency;

/// <summary>
/// 定义幂等请求占用和结果存储的能力边界
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// 尝试开始幂等请求处理并返回占用状态
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="fingerprint">用于区分幂等请求负载的指纹</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的幂等占用结果</returns>
    Task<IdempotencyReservation> TryBeginAsync(IdempotencyKey key, string fingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从测试替身中读取指定条目
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的幂等处理结果</returns>
    Task<IdempotencyResult?> GetAsync(IdempotencyKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将幂等请求标记为完成并保存结果
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="result">当前流程预置或返回的结果</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task CompleteAsync(IdempotencyKey key, IdempotencyResult result, CancellationToken cancellationToken = default);
}
