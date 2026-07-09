namespace Tw.AspNetCore.Security;

/// <summary>表示 AuthenticationBoundaryOptions 声明</summary>
public sealed record AuthenticationBoundaryOptions(
    string ValidIssuer,
    string ValidAudience,
    IReadOnlyList<string> RequiredScopes)
{
    /// <summary>执行 Validate 操作</summary>
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
