namespace Tw.DistributedLocking;

/// <summary>
/// 标识 provider-neutral 分布式锁资源
/// </summary>
public sealed record DistributedLockKey
{
    /// <summary>
    /// 使用非空白锁键值创建资源标识
    /// </summary>
    /// <param name="value">传递给锁提供程序的稳定键值</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> 为空或仅包含空白字符</exception>
    public DistributedLockKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// 传递给锁提供程序的稳定键值
    /// </summary>
    public string Value { get; }
}
