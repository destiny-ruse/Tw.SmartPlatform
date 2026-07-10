namespace Tw.MultiTenancy.Abstractions;

/// <summary>
/// 封装Current租户相关的数据和行为
/// </summary>
public sealed record CurrentTenant(string Id)
{
    /// <summary>
    /// new在当前对象中的业务含义
    /// </summary>
    public static CurrentTenant Default { get; } = new("default");
}
