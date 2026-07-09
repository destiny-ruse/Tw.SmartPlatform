using Tw.MultiTenancy.Abstractions;

namespace Tw.MultiTenancy;

/// <summary>表示 TenantResolver 类型</summary>
public sealed class TenantResolver
{
    /// <summary>执行 Resolve 操作</summary>
    /// <param name="tokenTenantId">tokenTenantId 参数</param>
    /// <param name="hintedTenantId">hintedTenantId 参数</param>
    /// <returns>Resolve 的执行结果</returns>
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
