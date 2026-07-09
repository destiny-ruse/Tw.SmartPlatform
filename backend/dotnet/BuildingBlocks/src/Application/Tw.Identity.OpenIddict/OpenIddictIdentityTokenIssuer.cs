namespace Tw.Identity.OpenIddict;

/// <summary>表示 OpenIddictIdentityTokenIssuer 类型</summary>
internal sealed class OpenIddictIdentityTokenIssuer : IIdentityTokenIssuer
{
    /// <summary>执行 IssueAsync 操作</summary>
    /// <param name="request">request 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>IssueAsync 的执行结果</returns>
    public Task<string> IssueAsync(IdentityTokenRequest request, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("token 发行必须由 Identity Center 宿主提供 OpenIddict 适配");
    }
}
