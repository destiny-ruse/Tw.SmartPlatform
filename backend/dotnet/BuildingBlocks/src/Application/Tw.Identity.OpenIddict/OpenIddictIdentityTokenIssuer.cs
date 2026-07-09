namespace Tw.Identity.OpenIddict;

internal sealed class OpenIddictIdentityTokenIssuer : IIdentityTokenIssuer
{
    public Task<string> IssueAsync(IdentityTokenRequest request, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("token 发行必须由 Identity Center 宿主提供 OpenIddict 适配");
    }
}
