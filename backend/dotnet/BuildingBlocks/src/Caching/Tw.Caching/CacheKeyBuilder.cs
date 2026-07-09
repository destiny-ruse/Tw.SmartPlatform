namespace Tw.Caching;

public static class CacheKeyBuilder
{
    public static CacheKey Build(string tenantId, string shardId, string resourceType, string resourceId, string version)
    {
        Validate(tenantId, shardId, resourceType, resourceId, version);
        return new CacheKey($"{tenantId}:{shardId}:{resourceType}:{resourceId}:{version}");
    }

    private static void Validate(params string[] values)
    {
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        }
    }
}
