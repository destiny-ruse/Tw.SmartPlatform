namespace Tw.MultiTenancy;

public sealed class TenantMismatchException : Exception
{
    public TenantMismatchException()
        : base("Tenant id does not match the authenticated token tenant.")
    {
    }
}
