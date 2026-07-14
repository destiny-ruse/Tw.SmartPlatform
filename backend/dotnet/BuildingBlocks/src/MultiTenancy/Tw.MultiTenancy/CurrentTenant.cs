namespace Tw.MultiTenancy;

/// <summary>
/// 表示认证后解析得到的当前租户身份
/// </summary>
/// <param name="Id">调用链中使用的不透明租户标识</param>
public sealed record CurrentTenant(string Id)
{
    /// <summary>
    /// 未解析到租户信息时使用的默认租户
    /// </summary>
    public static CurrentTenant Default { get; } = new("default");
}
