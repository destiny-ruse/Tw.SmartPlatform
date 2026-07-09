namespace Tw.Caching;

/// <summary>表示 CacheKeyBuilder 类型</summary>
public static class CacheKeyBuilder
{
    /// <summary>执行 Build 操作</summary>
    /// <param name="tenantId">tenantId 参数</param>
    /// <param name="shardId">shardId 参数</param>
    /// <param name="resourceType">resourceType 参数</param>
    /// <param name="resourceId">resourceId 参数</param>
    /// <param name="version">version 参数</param>
    /// <returns>Build 的执行结果</returns>
    public static CacheKey Build(string tenantId, string shardId, string resourceType, string resourceId, string version)
    {
        Validate(tenantId, shardId, resourceType, resourceId, version);
        return new CacheKey($"{tenantId}:{shardId}:{resourceType}:{resourceId}:{version}");
    }

    /// <summary>执行 Validate 操作</summary>
    /// <param name="values">values 参数</param>
    private static void Validate(params string[] values)
    {
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        }
    }
}
