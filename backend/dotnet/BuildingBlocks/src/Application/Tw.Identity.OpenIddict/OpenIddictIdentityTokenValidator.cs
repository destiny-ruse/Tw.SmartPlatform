namespace Tw.Identity.OpenIddict;

internal sealed class OpenIddictIdentityTokenValidator : IIdentityTokenValidator
{
    public Task<IdentityTokenValidationResult> ValidateAsync(
        IdentityTokenValidationRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("token 校验必须由 Identity Center 宿主提供 OpenIddict 适配");
    }
}
