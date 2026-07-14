namespace Tw.MultiTenancy;

/// <summary>
/// 令牌租户与提示租户标识不一致时抛出的异常
/// </summary>
public sealed class TenantMismatchException : Exception
{
    /// <summary>
    /// 使用稳定消息描述令牌租户与提示租户标识不一致
    /// </summary>
    public TenantMismatchException()
        : base("提示租户标识与认证令牌中的租户标识不一致。")
    {
    }
}
