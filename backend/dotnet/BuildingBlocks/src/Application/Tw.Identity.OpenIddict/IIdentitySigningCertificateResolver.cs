using System.Security.Cryptography.X509Certificates;

namespace Tw.Identity.OpenIddict;

/// <summary>
/// token 签名证书解析边界
/// </summary>
public interface IIdentitySigningCertificateResolver
{
    /// <summary>
    /// 按证书名称解析签名证书
    /// </summary>
    /// <param name="certificateName">签名证书名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>签名证书</returns>
    Task<X509Certificate2> ResolveAsync(string certificateName, CancellationToken cancellationToken);
}
