namespace Tw.Sharding.Abstractions;

/// <summary>
/// 定义Shard上下文的能力边界
/// </summary>
public interface IShardContext
{
    /// <summary>
    /// Current在当前对象中的业务含义
    /// </summary>
    ShardDescriptor Current { get; }
}
