namespace Tw.MultiTenancy;

/// <summary>
/// 根据认证令牌和调用方提示解析当前租户
/// </summary>
public sealed class TenantResolver
{
    /// <summary>
    /// 解析令牌租户与提示租户，并拒绝身份不一致的调用
    /// </summary>
    /// <param name="tokenTenantId">可信认证令牌中的租户标识</param>
    /// <param name="hintedTenantId">调用边界提供的租户提示标识</param>
    /// <returns>一致性检查后的当前租户；两个输入均缺失时返回默认租户</returns>
    /// <exception cref="TenantMismatchException">令牌租户与提示租户不一致时抛出</exception>
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
