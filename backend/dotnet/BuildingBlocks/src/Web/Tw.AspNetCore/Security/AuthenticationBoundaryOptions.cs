namespace Tw.AspNetCore.Security;

public sealed record AuthenticationBoundaryOptions(
    string ValidIssuer,
    string ValidAudience,
    IReadOnlyList<string> RequiredScopes)
{
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
