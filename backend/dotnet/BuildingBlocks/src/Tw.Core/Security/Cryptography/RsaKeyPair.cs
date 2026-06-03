namespace Tw.Core.Security.Cryptography;

/// <summary>
/// 表示编码为 PEM 的 RSA 公私钥对
/// </summary>
/// <param name="PublicKeyPem">PKCS#1 PEM 格式的公钥</param>
/// <param name="PrivateKeyPem">PKCS#1 PEM 格式的私钥</param>
public readonly record struct RsaKeyPair(string PublicKeyPem, string PrivateKeyPem);
