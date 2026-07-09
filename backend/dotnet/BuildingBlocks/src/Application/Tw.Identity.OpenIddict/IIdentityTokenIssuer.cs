namespace Tw.Identity.OpenIddict;

/// <summary>
/// token 发行边界
/// </summary>
public interface IIdentityTokenIssuer
{
    /// <summary>
    /// 按请求发行访问 token
    /// </summary>
    /// <param name="request">token 发行请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>访问 token</returns>
    Task<string> IssueAsync(IdentityTokenRequest request, CancellationToken cancellationToken);
}
