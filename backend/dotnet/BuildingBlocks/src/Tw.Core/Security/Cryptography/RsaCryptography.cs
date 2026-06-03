using System.Security.Cryptography;
using System.Text;
using Tw.Core;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// 提供 RSA 密钥生成、加密、解密、签名和签名验证辅助方法
/// </summary>
/// <remarks>
/// 加密默认使用带 SHA-256 的 OAEP。签名默认使用带 PKCS#1 签名填充的 SHA-256
/// </remarks>
public static class RsaCryptography
{
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly RSAEncryptionPadding DefaultEncryptionPadding = RSAEncryptionPadding.OaepSHA256;
    private static readonly HashAlgorithmName DefaultSignatureHashAlgorithm = HashAlgorithmName.SHA256;
    private static readonly RSASignaturePadding DefaultSignaturePadding = RSASignaturePadding.Pkcs1;

    /// <summary>生成编码为 PEM 的 RSA 密钥对</summary>
    /// <param name="keySize">RSA 密钥位数</param>
    /// <returns>生成的 RSA 密钥对</returns>
    public static RsaKeyPair GenerateKeyPair(int keySize = 2048)
    {
        Check.Positive(keySize);

        using var rsa = RSA.Create(keySize);
        return new RsaKeyPair(rsa.ExportRSAPublicKeyPem(), rsa.ExportRSAPrivateKeyPem());
    }

    /// <summary>生成编码为 DER 字节的 RSA 密钥对</summary>
    /// <param name="keySize">RSA 密钥位数</param>
    /// <returns>生成的 RSA 密钥对</returns>
    public static RsaDerKeyPair GenerateDerKeyPair(int keySize = 2048)
    {
        Check.Positive(keySize);

        using var rsa = RSA.Create(keySize);
        return new RsaDerKeyPair(rsa.ExportRSAPublicKey(), rsa.ExportRSAPrivateKey());
    }

    /// <summary>使用 PEM 公钥加密字符串并返回 Base64 密文</summary>
    /// <param name="input">要加密的文本</param>
    /// <param name="publicKeyPem">PEM 格式的公钥</param>
    /// <param name="padding">RSA 加密填充；省略时使用 OAEP SHA-256</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>Base64 编码的加密字节</returns>
    public static string Encrypt(
        string input,
        string publicKeyPem,
        RSAEncryptionPadding? padding = null,
        Encoding? encoding = null)
    {
        Check.NotNull(input);

        var encrypted = Encrypt((encoding ?? DefaultEncoding).GetBytes(input), publicKeyPem, padding);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>使用 PEM 公钥加密字节</summary>
    /// <param name="bytes">要加密的字节</param>
    /// <param name="publicKeyPem">PEM 格式的公钥</param>
    /// <param name="padding">RSA 加密填充；省略时使用 OAEP SHA-256</param>
    /// <returns>加密后的字节</returns>
    public static byte[] Encrypt(byte[] bytes, string publicKeyPem, RSAEncryptionPadding? padding = null)
    {
        Check.NotNull(bytes);
        Check.NotNullOrWhiteSpace(publicKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        return rsa.Encrypt(bytes, padding ?? DefaultEncryptionPadding);
    }

    /// <summary>使用 DER 公钥加密字节</summary>
    /// <param name="bytes">要加密的字节</param>
    /// <param name="publicKeyDer">DER 格式的公钥</param>
    /// <param name="padding">RSA 加密填充；省略时使用 OAEP SHA-256</param>
    /// <returns>加密后的字节</returns>
    public static byte[] Encrypt(byte[] bytes, byte[] publicKeyDer, RSAEncryptionPadding? padding = null)
    {
        Check.NotNull(bytes);
        Check.NotNull(publicKeyDer);

        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publicKeyDer, out _);
        return rsa.Encrypt(bytes, padding ?? DefaultEncryptionPadding);
    }

    /// <summary>使用 PEM 私钥解密 Base64 密文</summary>
    /// <param name="input">要解密的 Base64 密文</param>
    /// <param name="privateKeyPem">PEM 格式的私钥</param>
    /// <param name="padding">RSA 加密填充；省略时使用 OAEP SHA-256</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>解密后的文本</returns>
    public static string Decrypt(
        string input,
        string privateKeyPem,
        RSAEncryptionPadding? padding = null,
        Encoding? encoding = null)
    {
        Check.NotNullOrWhiteSpace(input);

        var decrypted = Decrypt(Convert.FromBase64String(input), privateKeyPem, padding);
        return (encoding ?? DefaultEncoding).GetString(decrypted);
    }

    /// <summary>使用 PEM 私钥解密字节</summary>
    /// <param name="bytes">要解密的字节</param>
    /// <param name="privateKeyPem">PEM 格式的私钥</param>
    /// <param name="padding">RSA 加密填充；省略时使用 OAEP SHA-256</param>
    /// <returns>解密后的字节</returns>
    public static byte[] Decrypt(byte[] bytes, string privateKeyPem, RSAEncryptionPadding? padding = null)
    {
        Check.NotNull(bytes);
        Check.NotNullOrWhiteSpace(privateKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        return rsa.Decrypt(bytes, padding ?? DefaultEncryptionPadding);
    }

    /// <summary>使用 DER 私钥解密字节</summary>
    /// <param name="bytes">要解密的字节</param>
    /// <param name="privateKeyDer">DER 格式的私钥</param>
    /// <param name="padding">RSA 加密填充；省略时使用 OAEP SHA-256</param>
    /// <returns>解密后的字节</returns>
    public static byte[] Decrypt(byte[] bytes, byte[] privateKeyDer, RSAEncryptionPadding? padding = null)
    {
        Check.NotNull(bytes);
        Check.NotNull(privateKeyDer);

        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKeyDer, out _);
        return rsa.Decrypt(bytes, padding ?? DefaultEncryptionPadding);
    }

    /// <summary>使用 PEM 私钥为字符串签名并返回 Base64 签名</summary>
    /// <param name="input">要签名的文本</param>
    /// <param name="privateKeyPem">PEM 格式的私钥</param>
    /// <param name="hashAlgorithm">签名哈希算法；省略时使用 SHA-256</param>
    /// <param name="padding">RSA 签名填充；省略时使用 PKCS#1</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>Base64 编码的签名</returns>
    public static string Sign(
        string input,
        string privateKeyPem,
        HashAlgorithmName? hashAlgorithm = null,
        RSASignaturePadding? padding = null,
        Encoding? encoding = null)
    {
        Check.NotNull(input);

        var signature = Sign((encoding ?? DefaultEncoding).GetBytes(input), privateKeyPem, hashAlgorithm, padding);
        return Convert.ToBase64String(signature);
    }

    /// <summary>使用 PEM 私钥为字节签名</summary>
    /// <param name="bytes">要签名的字节</param>
    /// <param name="privateKeyPem">PEM 格式的私钥</param>
    /// <param name="hashAlgorithm">签名哈希算法；省略时使用 SHA-256</param>
    /// <param name="padding">RSA 签名填充；省略时使用 PKCS#1</param>
    /// <returns>签名字节</returns>
    public static byte[] Sign(
        byte[] bytes,
        string privateKeyPem,
        HashAlgorithmName? hashAlgorithm = null,
        RSASignaturePadding? padding = null)
    {
        Check.NotNull(bytes);
        Check.NotNullOrWhiteSpace(privateKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        return rsa.SignData(bytes, hashAlgorithm ?? DefaultSignatureHashAlgorithm, padding ?? DefaultSignaturePadding);
    }

    /// <summary>使用 PEM 公钥验证字符串的 Base64 签名</summary>
    /// <param name="input">要验证签名的文本</param>
    /// <param name="signature">Base64 签名</param>
    /// <param name="publicKeyPem">PEM 格式的公钥</param>
    /// <param name="hashAlgorithm">签名哈希算法；省略时使用 SHA-256</param>
    /// <param name="padding">RSA 签名填充；省略时使用 PKCS#1</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>签名有效时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool VerifySignature(
        string input,
        string signature,
        string publicKeyPem,
        HashAlgorithmName? hashAlgorithm = null,
        RSASignaturePadding? padding = null,
        Encoding? encoding = null)
    {
        Check.NotNull(input);
        Check.NotNullOrWhiteSpace(signature);

        byte[] signatureBytes;

        try
        {
            signatureBytes = Convert.FromBase64String(signature);
        }
        catch (FormatException)
        {
            return false;
        }

        return VerifySignature(
            (encoding ?? DefaultEncoding).GetBytes(input),
            signatureBytes,
            publicKeyPem,
            hashAlgorithm,
            padding);
    }

    /// <summary>使用 PEM 公钥验证字节签名</summary>
    /// <param name="bytes">要验证签名的字节</param>
    /// <param name="signature">签名字节</param>
    /// <param name="publicKeyPem">PEM 格式的公钥</param>
    /// <param name="hashAlgorithm">签名哈希算法；省略时使用 SHA-256</param>
    /// <param name="padding">RSA 签名填充；省略时使用 PKCS#1</param>
    /// <returns>签名有效时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool VerifySignature(
        byte[] bytes,
        byte[] signature,
        string publicKeyPem,
        HashAlgorithmName? hashAlgorithm = null,
        RSASignaturePadding? padding = null)
    {
        Check.NotNull(bytes);
        Check.NotNull(signature);
        Check.NotNullOrWhiteSpace(publicKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        return rsa.VerifyData(bytes, signature, hashAlgorithm ?? DefaultSignatureHashAlgorithm, padding ?? DefaultSignaturePadding);
    }
}
