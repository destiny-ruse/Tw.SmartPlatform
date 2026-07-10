namespace Tw.AspNetCore.Security;

/// <summary>
/// 配置认证边界的运行行为
/// </summary>
public sealed record AuthenticationBoundaryOptions(
    string ValidIssuer,
    string ValidAudience,
    IReadOnlyList<string> RequiredScopes)
{
    /// <summary>
    /// 校验当前配置或输入约束，并在非法时抛出异常
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ValidIssuer))
        {
            throw new InvalidOperationException("JWT issuer must be configured");
        }

        if (string.IsNullOrWhiteSpace(ValidAudience))
        {
            throw new InvalidOperationException("JWT audience must be configured");
        }
    }
}
