namespace Tw.Settings;

/// <summary>
/// Setting 值作用域
/// </summary>
public enum SettingScope
{
    /// <summary>
    /// 服务默认作用域
    /// </summary>
    Service = 1,

    /// <summary>
    /// 租户作用域
    /// </summary>
    Tenant = 2,

    /// <summary>
    /// 用户作用域
    /// </summary>
    User = 3
}
