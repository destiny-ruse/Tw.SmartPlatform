namespace Tw.Sharding.Abstractions;

/// <summary>定义 IShardContext 契约</summary>
public interface IShardContext
{
    /// <summary>表示 Current 属性</summary>
    ShardDescriptor Current { get; }
}
