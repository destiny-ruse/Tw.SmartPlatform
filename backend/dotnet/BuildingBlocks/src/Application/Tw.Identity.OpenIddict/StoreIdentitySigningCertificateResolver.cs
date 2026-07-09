using System.Security.Cryptography.X509Certificates;

namespace Tw.Identity.OpenIddict;

/// <summary>表示 StoreIdentitySigningCertificateResolver 类型</summary>
internal sealed class StoreIdentitySigningCertificateResolver : IIdentitySigningCertificateResolver
{
    /// <summary>执行 ResolveAsync 操作</summary>
    /// <param name="certificateName">certificateName 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ResolveAsync 的执行结果</returns>
    public Task<X509Certificate2> ResolveAsync(string certificateName, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("签名证书解析必须由 Identity Center 宿主提供存储适配");
    }
}
