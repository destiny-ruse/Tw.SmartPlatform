namespace Tw.Sharding.Abstractions;

public sealed record ShardDescriptor(string Strategy, string Key)
{
    public static ShardDescriptor None { get; } = new("none", "default");
}
