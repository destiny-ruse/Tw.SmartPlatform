namespace Tw.Identity.OpenIddict;

/// <summary>
/// token 校验边界
/// </summary>
public interface IIdentityTokenValidator
{
    /// <summary>
    /// 校验访问 token
    /// </summary>
    /// <param name="request">token 校验请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>token 校验结果</returns>
    Task<IdentityTokenValidationResult> ValidateAsync(
        IdentityTokenValidationRequest request,
        CancellationToken cancellationToken);
}
