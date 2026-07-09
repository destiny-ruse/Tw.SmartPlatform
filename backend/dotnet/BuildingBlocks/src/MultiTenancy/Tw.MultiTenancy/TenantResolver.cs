using Tw.MultiTenancy.Abstractions;

namespace Tw.MultiTenancy;

public sealed class TenantResolver
{
    public CurrentTenant Resolve(string? tokenTenantId, string? hintedTenantId)
    {
        if (!string.IsNullOrWhiteSpace(tokenTenantId)
            && !string.IsNullOrWhiteSpace(hintedTenantId)
            && !string.Equals(tokenTenantId, hintedTenantId, StringComparison.Ordinal))
        {
            throw new TenantMismatchException();
        }

        return new CurrentTenant(tokenTenantId ?? hintedTenantId ?? CurrentTenant.Default.Id);
    }
}
