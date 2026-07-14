namespace Tw.Sharding;

/// <summary>
/// 表示提供方无关的分片策略与分片键
/// </summary>
/// <param name="Strategy">调用方选择的分片策略标识</param>
/// <param name="Key">调用方选择的不透明分片键</param>
public sealed record ShardDescriptor(string Strategy, string Key)
{
    /// <summary>
    /// 未选择具体分片时使用的空分片描述
    /// </summary>
    public static ShardDescriptor None { get; } = new("none", "default");
}
