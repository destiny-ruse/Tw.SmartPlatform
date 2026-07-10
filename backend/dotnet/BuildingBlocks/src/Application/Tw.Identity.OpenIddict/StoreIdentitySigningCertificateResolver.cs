using System.Security.Cryptography.X509Certificates;

namespace Tw.Identity.OpenIddict;

/// <summary>
/// 封装存储身份SigningCertificateResolver相关的数据和行为
/// </summary>
internal sealed class StoreIdentitySigningCertificateResolver : IIdentitySigningCertificateResolver
{
    /// <summary>
    /// 解析测试场景所需的签名证书
    /// </summary>
    /// <param name="certificateName">用于提供certificateName</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的X509Certificate2</returns>
    public Task<X509Certificate2> ResolveAsync(string certificateName, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("签名证书解析必须由 Identity Center 宿主提供存储适配");
    }
}
