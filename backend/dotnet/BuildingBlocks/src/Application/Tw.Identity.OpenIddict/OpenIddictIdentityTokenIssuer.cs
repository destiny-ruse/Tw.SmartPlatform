namespace Tw.Identity.OpenIddict;

/// <summary>
/// 封装OpenIddict身份令牌签发方相关的数据和行为
/// </summary>
internal sealed class OpenIddictIdentityTokenIssuer : IIdentityTokenIssuer
{
    /// <summary>
    /// 判断sue异步是否满足条件
    /// </summary>
    /// <param name="request">用于提供请求</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
    public Task<string> IssueAsync(IdentityTokenRequest request, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("token 发行必须由 Identity Center 宿主提供 OpenIddict 适配");
    }
}
