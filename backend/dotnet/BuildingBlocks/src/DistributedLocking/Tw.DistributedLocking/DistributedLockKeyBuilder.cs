using Tw.DistributedLocking.Abstractions;

namespace Tw.DistributedLocking;

/// <summary>表示 DistributedLockKeyBuilder 类型</summary>
public static class DistributedLockKeyBuilder
{
    /// <summary>执行 Build 操作</summary>
    /// <param name="tenantId">tenantId 参数</param>
    /// <param name="shardId">shardId 参数</param>
    /// <param name="resourceType">resourceType 参数</param>
    /// <param name="identifier">identifier 参数</param>
    /// <returns>Build 的执行结果</returns>
    public static DistributedLockKey Build(string tenantId, string shardId, string resourceType, string identifier)
    {
        Validate(tenantId, shardId, resourceType, identifier);
        return new DistributedLockKey($"lock:{tenantId}:{shardId}:{resourceType}:{identifier}");
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
