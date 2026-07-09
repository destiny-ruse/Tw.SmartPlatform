using System.Security.Cryptography;
using System.Text;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// 提供字符串使用密码学辅助方法的便利扩展
/// </summary>
public static class StringCryptographyExtensions
{
    /// <summary>计算字符串的 MD5 哈希</summary>
    public static string ComputeMd5Hash(this string input, bool useUpperCase = false, bool useShortHash = false, Encoding? encoding = null) =>
        Md5Hasher.ComputeHash(input, useUpperCase, useShortHash, encoding);

    /// <summary>计算字符串的 SHA-1 哈希</summary>
    public static string ComputeSha1Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha1Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>计算字符串的 SHA-256 哈希</summary>
    public static string ComputeSha256Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha256Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>计算字符串的 SHA-384 哈希</summary>
    public static string ComputeSha384Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha384Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>计算字符串的 SHA-512 哈希</summary>
    public static string ComputeSha512Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha512Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>计算字符串的 SHA3-256 哈希</summary>
    public static string ComputeSha3256Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha3256Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>计算字符串的 SHA3-384 哈希</summary>
    public static string ComputeSha3384Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha3384Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>计算字符串的 SHA3-512 哈希</summary>
    public static string ComputeSha3512Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha3512Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>验证字符串的 MD5 哈希</summary>
    public static bool VerifyMd5Hash(this string input, string hash, bool useShortHash = false, Encoding? encoding = null) =>
        Md5Hasher.VerifyHash(input, hash, useShortHash, encoding);

    /// <summary>验证字符串的 SHA-1 哈希</summary>
    public static bool VerifySha1Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha1Hasher.VerifyHash(input, hash, encoding);

    /// <summary>验证字符串的 SHA-256 哈希</summary>
    public static bool VerifySha256Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha256Hasher.VerifyHash(input, hash, encoding);

    /// <summary>验证字符串的 SHA-384 哈希</summary>
    public static bool VerifySha384Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha384Hasher.VerifyHash(input, hash, encoding);

    /// <summary>验证字符串的 SHA-512 哈希</summary>
    public static bool VerifySha512Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha512Hasher.VerifyHash(input, hash, encoding);

    /// <summary>验证字符串的 SHA3-256 哈希</summary>
    public static bool VerifySha3256Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha3256Hasher.VerifyHash(input, hash, encoding);

    /// <summary>验证字符串的 SHA3-384 哈希</summary>
    public static bool VerifySha3384Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha3384Hasher.VerifyHash(input, hash, encoding);

    /// <summary>验证字符串的 SHA3-512 哈希</summary>
    public static bool VerifySha3512Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha3512Hasher.VerifyHash(input, hash, encoding);

    /// <summary>计算字符串的 HMAC-MD5 哈希</summary>
    public static string ComputeHmacMd5Hash(
        this string input,
        string key,
        bool useUpperCase = false,
        bool useShortHash = false,
        Encoding? encoding = null) =>
        HmacMd5Hasher.ComputeHash(key, input, useUpperCase, useShortHash, encoding);

    /// <summary>计算字符串的 HMAC-SHA-1 哈希</summary>
    public static string ComputeHmacSha1Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha1Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>计算字符串的 HMAC-SHA-256 哈希</summary>
    public static string ComputeHmacSha256Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha256Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>计算字符串的 HMAC-SHA-384 哈希</summary>
    public static string ComputeHmacSha384Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha384Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>计算字符串的 HMAC-SHA-512 哈希</summary>
    public static string ComputeHmacSha512Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha512Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>计算字符串的 HMAC-SHA3-256 哈希</summary>
    public static string ComputeHmacSha3256Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha3256Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>计算字符串的 HMAC-SHA3-384 哈希</summary>
    public static string ComputeHmacSha3384Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha3384Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>计算字符串的 HMAC-SHA3-512 哈希</summary>
    public static string ComputeHmacSha3512Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha3512Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>验证字符串的 HMAC-MD5 哈希</summary>
    public static bool VerifyHmacMd5Hash(
        this string input,
        string key,
        string hash,
        bool useShortHash = false,
        Encoding? encoding = null) =>
        HmacMd5Hasher.VerifyHash(key, input, hash, useShortHash, encoding);

    /// <summary>验证字符串的 HMAC-SHA-1 哈希</summary>
    public static bool VerifyHmacSha1Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha1Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>验证字符串的 HMAC-SHA-256 哈希</summary>
    public static bool VerifyHmacSha256Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha256Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>验证字符串的 HMAC-SHA-384 哈希</summary>
    public static bool VerifyHmacSha384Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha384Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>验证字符串的 HMAC-SHA-512 哈希</summary>
    public static bool VerifyHmacSha512Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha512Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>验证字符串的 HMAC-SHA3-256 哈希</summary>
    public static bool VerifyHmacSha3256Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha3256Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>验证字符串的 HMAC-SHA3-384 哈希</summary>
    public static bool VerifyHmacSha3384Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha3384Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>验证字符串的 HMAC-SHA3-512 哈希</summary>
    public static bool VerifyHmacSha3512Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha3512Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>使用 AES 加密字符串并返回 Base64 密文</summary>
    public static string EncryptWithAes(
        this string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null) =>
        AesCryptography.Encrypt(input, key, iv, mode, padding, isKeyBase64, encoding);

    /// <summary>将 AES Base64 密文解密为字符串</summary>
    public static string DecryptWithAes(
        this string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null) =>
        AesCryptography.Decrypt(input, key, iv, mode, padding, isKeyBase64, encoding);

    /// <summary>使用 DES 加密字符串并返回 Base64 密文</summary>
    public static string EncryptWithDes(
        this string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null) =>
        DesCryptography.Encrypt(input, key, iv, mode, padding, isKeyBase64, encoding);

    /// <summary>将 DES Base64 密文解密为字符串</summary>
    public static string DecryptWithDes(
        this string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null) =>
        DesCryptography.Decrypt(input, key, iv, mode, padding, isKeyBase64, encoding);

    /// <summary>使用 TripleDES 加密字符串并返回 Base64 密文</summary>
    public static string EncryptWithTripleDes(
        this string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null) =>
        TripleDesCryptography.Encrypt(input, key, iv, mode, padding, isKeyBase64, encoding);

    /// <summary>将 TripleDES Base64 密文解密为字符串</summary>
    public static string DecryptWithTripleDes(
        this string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null) =>
        TripleDesCryptography.Decrypt(input, key, iv, mode, padding, isKeyBase64, encoding);

    /// <summary>使用 RSA 公钥加密字符串并返回 Base64 密文</summary>
    public static string EncryptWithRsa(
        this string input,
        string publicKeyPem,
        RSAEncryptionPadding? padding = null,
        Encoding? encoding = null) =>
        RsaCryptography.Encrypt(input, publicKeyPem, padding, encoding);

    /// <summary>使用私钥解密 RSA Base64 密文</summary>
    public static string DecryptWithRsa(
        this string input,
        string privateKeyPem,
        RSAEncryptionPadding? padding = null,
        Encoding? encoding = null) =>
        RsaCryptography.Decrypt(input, privateKeyPem, padding, encoding);

    /// <summary>使用 RSA 私钥为字符串签名并返回 Base64 签名</summary>
    public static string SignWithRsa(
        this string input,
        string privateKeyPem,
        HashAlgorithmName? hashAlgorithm = null,
        RSASignaturePadding? padding = null,
        Encoding? encoding = null) =>
        RsaCryptography.Sign(input, privateKeyPem, hashAlgorithm, padding, encoding);

    /// <summary>验证字符串的 Base64 RSA 签名</summary>
    public static bool VerifyRsaSignature(
        this string input,
        string signature,
        string publicKeyPem,
        HashAlgorithmName? hashAlgorithm = null,
        RSASignaturePadding? padding = null,
        Encoding? encoding = null) =>
        RsaCryptography.VerifySignature(input, signature, publicKeyPem, hashAlgorithm, padding, encoding);

    /// <summary>使用 PBKDF2 哈希密码</summary>
    public static string HashPasswordWithPbkdf2(
        this string password,
        int iterations = 100000,
        int keyLength = 32,
        int saltLength = 16,
        HashAlgorithmName? hashAlgorithm = null,
        Encoding? encoding = null) =>
        Pbkdf2PasswordHasher.HashPassword(password, iterations, keyLength, saltLength, hashAlgorithm, encoding);

    /// <summary>根据 PBKDF2 密码哈希验证密码</summary>
    public static bool VerifyPbkdf2Password(
        this string password,
        string hashedPassword,
        int iterations = 100000,
        HashAlgorithmName? hashAlgorithm = null,
        Encoding? encoding = null) =>
        Pbkdf2PasswordHasher.VerifyPassword(password, hashedPassword, iterations, hashAlgorithm, encoding);
}
