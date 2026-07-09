namespace Tw.Caching;

/// <summary>定义 ICacheInvalidationPublisher 契约</summary>
public interface ICacheInvalidationPublisher
{
    /// <summary>执行 PublishAsync 操作</summary>
    /// <param name="key">key 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>PublishAsync 的执行结果</returns>
    Task PublishAsync(CacheKey key, CancellationToken cancellationToken = default);
}
