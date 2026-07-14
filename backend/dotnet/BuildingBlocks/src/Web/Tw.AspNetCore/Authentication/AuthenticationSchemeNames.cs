namespace Tw.AspNetCore.Authentication;

/// <summary>
/// 提供 ASP.NET Core 认证边界使用的标准方案名称
/// </summary>
public static class AuthenticationSchemeNames
{
    /// <summary>
    /// HTTP Bearer 令牌认证方案名称
    /// </summary>
    public const string Bearer = "Bearer";

    /// <summary>
    /// Cookie 认证方案名称
    /// </summary>
    public const string Cookies = "Cookies";
}
