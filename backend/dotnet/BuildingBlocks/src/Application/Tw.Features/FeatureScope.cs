namespace Tw.Features;

/// <summary>
/// Feature 值作用域
/// </summary>
public enum FeatureScope
{
    /// <summary>服务默认值</summary>
    Service = 1,

    /// <summary>租户覆盖值</summary>
    Tenant = 2,

    /// <summary>用户覆盖值</summary>
    User = 3
}
