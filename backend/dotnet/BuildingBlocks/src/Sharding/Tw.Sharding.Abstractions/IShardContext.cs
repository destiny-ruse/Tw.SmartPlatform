namespace Tw.Sharding.Abstractions;

public interface IShardContext
{
    ShardDescriptor Current { get; }
}
