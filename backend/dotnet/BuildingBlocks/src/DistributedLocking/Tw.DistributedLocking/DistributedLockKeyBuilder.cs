using Tw.DistributedLocking.Abstractions;

namespace Tw.DistributedLocking;

/// <summary>
/// 封装DistributedLock键构建器相关的数据和行为
/// </summary>
public static class DistributedLockKeyBuilder
{
    /// <summary>
    /// 说明Build在当前类型中的职责
    /// </summary>
    /// <param name="tenantId">用于提供tenant标识</param>
    /// <param name="shardId">用于提供shard标识</param>
    /// <param name="resourceType">用于提供resource类型</param>
    /// <param name="identifier">用于提供dentifier</param>
    /// <returns>方法计算得到的文本值</returns>
    public static DistributedLockKey Build(string tenantId, string shardId, string resourceType, string identifier)
    {
        Validate(tenantId, shardId, resourceType, identifier);
        return new DistributedLockKey($"lock:{tenantId}:{shardId}:{resourceType}:{identifier}");
    }

    /// <summary>
    /// 校验当前配置或输入约束，并在非法时抛出异常
    /// </summary>
    /// <param name="values">用于提供values</param>
    private static void Validate(params string[] values)
    {
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        }
    }
}
