namespace Tw.DistributedLocking;

/// <summary>
/// 按租户、分片、资源类型与资源标识构造稳定锁键
/// </summary>
public static class DistributedLockKeyBuilder
{
    /// <summary>
    /// 将资源边界组合为 provider-neutral 分布式锁键
    /// </summary>
    /// <param name="tenantId">资源所属租户标识</param>
    /// <param name="shardId">资源所属分片标识</param>
    /// <param name="resourceType">参与互斥的资源类型</param>
    /// <param name="identifier">资源在类型范围内的唯一标识</param>
    /// <returns>以 <c>lock:</c> 为前缀的稳定资源键</returns>
    /// <exception cref="ArgumentException">任一键段为空或仅包含空白字符</exception>
    public static DistributedLockKey Build(
        string tenantId,
        string shardId,
        string resourceType,
        string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        return new DistributedLockKey($"lock:{tenantId}:{shardId}:{resourceType}:{identifier}");
    }
}
