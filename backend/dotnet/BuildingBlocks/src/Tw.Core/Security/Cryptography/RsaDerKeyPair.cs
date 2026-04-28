namespace Tw.Core.Security.Cryptography;

/// <summary>
/// 表示编码为 DER 字节的 RSA 公私钥对
/// </summary>
/// <param name="PublicKeyDer">PKCS#1 DER 格式的公钥</param>
/// <param name="PrivateKeyDer">PKCS#1 DER 格式的私钥</param>
public readonly record struct RsaDerKeyPair(byte[] PublicKeyDer, byte[] PrivateKeyDer);
