namespace Tw.AspNetCore.Abstractions;

/// <summary>
/// 封装认证Scheme名称集合相关的数据和行为
/// </summary>
public static class AuthenticationSchemeNames
{
    /// <summary>
    /// 当前类型内部复用的Bearer常量值
    /// </summary>
    public const string Bearer = "Bearer";

    /// <summary>
    /// 当前类型内部复用的Cookies常量值
    /// </summary>
    public const string Cookies = "Cookies";
}
