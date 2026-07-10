namespace Tw.MultiTenancy;

/// <summary>
/// 封装租户Mismatch异常相关的数据和行为
/// </summary>
public sealed class TenantMismatchException : Exception
{
    /// <summary>
    /// 初始化 TenantMismatchException 实例
    /// </summary>
    public TenantMismatchException()
        : base("Tenant id does not match the authenticated token tenant.")
    {
    }
}
