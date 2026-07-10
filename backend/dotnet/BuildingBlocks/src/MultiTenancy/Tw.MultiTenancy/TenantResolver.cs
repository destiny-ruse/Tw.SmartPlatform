using Tw.MultiTenancy.Abstractions;

namespace Tw.MultiTenancy;

/// <summary>
/// 封装租户Resolver相关的数据和行为
/// </summary>
public sealed class TenantResolver
{
    /// <summary>
    /// 说明解析在当前类型中的职责
    /// </summary>
    /// <param name="tokenTenantId">用于提供tokenTenant标识</param>
    /// <param name="hintedTenantId">用于提供hintedTenant标识</param>
    /// <returns>方法计算得到的文本值</returns>
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
