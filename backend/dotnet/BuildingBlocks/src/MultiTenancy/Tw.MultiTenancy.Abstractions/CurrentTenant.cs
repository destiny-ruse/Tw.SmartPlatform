namespace Tw.MultiTenancy.Abstractions;

/// <summary>表示 CurrentTenant 声明</summary>
public sealed record CurrentTenant(string Id)
{
    /// <summary>表示 Default 属性</summary>
    public static CurrentTenant Default { get; } = new("default");
}
