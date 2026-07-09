namespace Tw.Sharding.Abstractions;

/// <summary>表示 ShardDescriptor 声明</summary>
public sealed record ShardDescriptor(string Strategy, string Key)
{
    /// <summary>表示 None 属性</summary>
    public static ShardDescriptor None { get; } = new("none", "default");
}
