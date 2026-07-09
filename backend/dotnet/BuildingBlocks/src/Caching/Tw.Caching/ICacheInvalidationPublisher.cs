namespace Tw.Caching;

public interface ICacheInvalidationPublisher
{
    Task PublishAsync(CacheKey key, CancellationToken cancellationToken = default);
}
