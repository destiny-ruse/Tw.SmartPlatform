namespace Tw.MultiTenancy;

/// <summary>表示 TenantMismatchException 类型</summary>
public sealed class TenantMismatchException : Exception
{
    /// <summary>初始化 TenantMismatchException 实例</summary>
    public TenantMismatchException()
        : base("Tenant id does not match the authenticated token tenant.")
    {
    }
}
