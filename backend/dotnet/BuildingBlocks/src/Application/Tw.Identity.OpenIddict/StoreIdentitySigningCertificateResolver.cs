using System.Security.Cryptography.X509Certificates;

namespace Tw.Identity.OpenIddict;

internal sealed class StoreIdentitySigningCertificateResolver : IIdentitySigningCertificateResolver
{
    public Task<X509Certificate2> ResolveAsync(string certificateName, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("签名证书解析必须由 Identity Center 宿主提供存储适配");
    }
}
