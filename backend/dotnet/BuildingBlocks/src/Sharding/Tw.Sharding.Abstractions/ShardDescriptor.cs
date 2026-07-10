namespace Tw.Sharding.Abstractions;

/// <summary>
/// 封装ShardDescriptor相关的数据和行为
/// </summary>
public sealed record ShardDescriptor(string Strategy, string Key)
{
    /// <summary>
    /// new在当前对象中的业务含义
    /// </summary>
    public static ShardDescriptor None { get; } = new("none", "default");
}
