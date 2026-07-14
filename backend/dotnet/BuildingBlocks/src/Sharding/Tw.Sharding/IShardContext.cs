namespace Tw.Sharding;

/// <summary>
/// 提供当前异步调用链选定的分片描述
/// </summary>
public interface IShardContext
{
    /// <summary>
    /// 当前作用域的分片描述；未指定时为空分片描述
    /// </summary>
    ShardDescriptor Current { get; }
}
