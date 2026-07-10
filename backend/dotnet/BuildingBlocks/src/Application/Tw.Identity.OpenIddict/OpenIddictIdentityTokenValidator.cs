namespace Tw.Identity.OpenIddict;

/// <summary>
/// 封装OpenIddict身份令牌Validator相关的数据和行为
/// </summary>
internal sealed class OpenIddictIdentityTokenValidator : IIdentityTokenValidator
{
    /// <summary>
    /// 校验异步并在非法时抛出异常
    /// </summary>
    /// <param name="request">用于提供请求</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的身份令牌Validation结果</returns>
    public Task<IdentityTokenValidationResult> ValidateAsync(
        IdentityTokenValidationRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("token 校验必须由 Identity Center 宿主提供 OpenIddict 适配");
    }
}
