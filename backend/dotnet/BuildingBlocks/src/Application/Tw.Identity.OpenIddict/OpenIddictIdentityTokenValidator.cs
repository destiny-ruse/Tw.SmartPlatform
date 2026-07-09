namespace Tw.Identity.OpenIddict;

/// <summary>表示 OpenIddictIdentityTokenValidator 类型</summary>
internal sealed class OpenIddictIdentityTokenValidator : IIdentityTokenValidator
{
    /// <summary>执行 ValidateAsync 操作</summary>
    /// <param name="request">request 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ValidateAsync 的执行结果</returns>
    public Task<IdentityTokenValidationResult> ValidateAsync(
        IdentityTokenValidationRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("token 校验必须由 Identity Center 宿主提供 OpenIddict 适配");
    }
}
