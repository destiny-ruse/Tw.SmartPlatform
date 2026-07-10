namespace Tw.Caching;

/// <summary>
/// 定义缓存InvalidationPublisher的能力边界
/// </summary>
public interface ICacheInvalidationPublisher
{
    /// <summary>
    /// 发布集成事件到测试事件总线
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task PublishAsync(CacheKey key, CancellationToken cancellationToken = default);
}
