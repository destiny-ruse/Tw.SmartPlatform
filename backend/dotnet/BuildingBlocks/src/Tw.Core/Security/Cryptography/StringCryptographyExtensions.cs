using System.Security.Cryptography;
using System.Text;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// Provides string convenience extensions for cryptography helpers.
/// </summary>
public static class StringCryptographyExtensions
{
    /// <summary>Computes the MD5 hash for a string.</summary>
    public static string ComputeMd5Hash(this string input, bool useUpperCase = false, bool useShortHash = false, Encoding? encoding = null) =>
        Md5Hasher.ComputeHash(input, useUpperCase, useShortHash, encoding);

    /// <summary>Computes the SHA-1 hash for a string.</summary>
    public static string ComputeSha1Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha1Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>Computes the SHA-256 hash for a string.</summary>
    public static string ComputeSha256Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha256Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>Computes the SHA-384 hash for a string.</summary>
    public static string ComputeSha384Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha384Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>Computes the SHA-512 hash for a string.</summary>
    public static string ComputeSha512Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha512Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>Computes the SHA3-256 hash for a string.</summary>
    public static string ComputeSha3256Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha3256Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>Computes the SHA3-384 hash for a string.</summary>
    public static string ComputeSha3384Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha3384Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>Computes the SHA3-512 hash for a string.</summary>
    public static string ComputeSha3512Hash(this string input, bool useUpperCase = false, Encoding? encoding = null) =>
        Sha3512Hasher.ComputeHash(input, useUpperCase, encoding);

    /// <summary>Verifies the MD5 hash for a string.</summary>
    public static bool VerifyMd5Hash(this string input, string hash, bool useShortHash = false, Encoding? encoding = null) =>
        Md5Hasher.VerifyHash(input, hash, useShortHash, encoding);

    /// <summary>Verifies the SHA-1 hash for a string.</summary>
    public static bool VerifySha1Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha1Hasher.VerifyHash(input, hash, encoding);

    /// <summary>Verifies the SHA-256 hash for a string.</summary>
    public static bool VerifySha256Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha256Hasher.VerifyHash(input, hash, encoding);

    /// <summary>Verifies the SHA-384 hash for a string.</summary>
    public static bool VerifySha384Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha384Hasher.VerifyHash(input, hash, encoding);

    /// <summary>Verifies the SHA-512 hash for a string.</summary>
    public static bool VerifySha512Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha512Hasher.VerifyHash(input, hash, encoding);

    /// <summary>Verifies the SHA3-256 hash for a string.</summary>
    public static bool VerifySha3256Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha3256Hasher.VerifyHash(input, hash, encoding);

    /// <summary>Verifies the SHA3-384 hash for a string.</summary>
    public static bool VerifySha3384Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha3384Hasher.VerifyHash(input, hash, encoding);

    /// <summary>Verifies the SHA3-512 hash for a string.</summary>
    public static bool VerifySha3512Hash(this string input, string hash, Encoding? encoding = null) =>
        Sha3512Hasher.VerifyHash(input, hash, encoding);

    /// <summary>Computes the HMAC-MD5 hash for a string.</summary>
    public static string ComputeHmacMd5Hash(
        this string input,
        string key,
        bool useUpperCase = false,
        bool useShortHash = false,
        Encoding? encoding = null) =>
        HmacMd5Hasher.ComputeHash(key, input, useUpperCase, useShortHash, encoding);

    /// <summary>Computes the HMAC-SHA-1 hash for a string.</summary>
    public static string ComputeHmacSha1Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha1Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>Computes the HMAC-SHA-256 hash for a string.</summary>
    public static string ComputeHmacSha256Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha256Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>Computes the HMAC-SHA-384 hash for a string.</summary>
    public static string ComputeHmacSha384Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha384Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>Computes the HMAC-SHA-512 hash for a string.</summary>
    public static string ComputeHmacSha512Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha512Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>Computes the HMAC-SHA3-256 hash for a string.</summary>
    public static string ComputeHmacSha3256Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha3256Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>Computes the HMAC-SHA3-384 hash for a string.</summary>
    public static string ComputeHmacSha3384Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha3384Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>Computes the HMAC-SHA3-512 hash for a string.</summary>
    public static string ComputeHmacSha3512Hash(this string input, string key, bool useUpperCase = false, Encoding? encoding = null) =>
        HmacSha3512Hasher.ComputeHash(key, input, useUpperCase, encoding);

    /// <summary>Verifies the HMAC-MD5 hash for a string.</summary>
    public static bool VerifyHmacMd5Hash(
        this string input,
        string key,
        string hash,
        bool useShortHash = false,
        Encoding? encoding = null) =>
        HmacMd5Hasher.VerifyHash(key, input, hash, useShortHash, encoding);

    /// <summary>Verifies the HMAC-SHA-1 hash for a string.</summary>
    public static bool VerifyHmacSha1Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha1Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>Verifies the HMAC-SHA-256 hash for a string.</summary>
    public static bool VerifyHmacSha256Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha256Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>Verifies the HMAC-SHA-384 hash for a string.</summary>
    public static bool VerifyHmacSha384Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha384Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>Verifies the HMAC-SHA-512 hash for a string.</summary>
    public static bool VerifyHmacSha512Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha512Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>Verifies the HMAC-SHA3-256 hash for a string.</summary>
    public static bool VerifyHmacSha3256Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha3256Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>Verifies the HMAC-SHA3-384 hash for a string.</summary>
    public static bool VerifyHmacSha3384Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha3384Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>Verifies the HMAC-SHA3-512 hash for a string.</summary>
    public static bool VerifyHmacSha3512Hash(this string input, string key, string hash, Encoding? encoding = null) =>
        HmacSha3512Hasher.VerifyHash(key, input, hash, encoding);

    /// <summary>Encrypts a string with AES and returns Base64 ciphertext.</summary>
    public static string EncryptWithAes(
        this string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null) =>
        AesCryptography.Encrypt(input, key, iv, mode, padding, isKeyBase64, encoding);

    /// <summary>Decrypts AES Base64 ciphertext to a string.</summary>
    public static string DecryptWithAes(
        this string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null) =>
        AesCryptography.Decrypt(input, key, iv, mode, padding, isKeyBase64, encoding);

    /// <summary>Encrypts a string with DES and returns Base64 ciphertext.</summary>
    public static string EncryptWithDes(
        this string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null) =>
        DesCryptography.Encrypt(input, key, iv, mode, padding, isKeyBase64, encoding);

    /// <summary>Decrypts DES Base64 ciphertext to a string.</summary>
    public static string DecryptWithDes(
        this string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null) =>
        DesCryptography.Decrypt(input, key, iv, mode, padding, isKeyBase64, encoding);

    /// <summary>Encrypts a string with TripleDES and returns Base64 ciphertext.</summary>
    public static string EncryptWithTripleDes(
        this string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null) =>
        TripleDesCryptography.Encrypt(input, key, iv, mode, padding, isKeyBase64, encoding);

    /// <summary>Decrypts TripleDES Base64 ciphertext to a string.</summary>
    public static string DecryptWithTripleDes(
        this string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null) =>
        TripleDesCryptography.Decrypt(input, key, iv, mode, padding, isKeyBase64, encoding);

    /// <summary>Encrypts a string with an RSA public key and returns Base64 ciphertext.</summary>
    public static string EncryptWithRsa(
        this string input,
        string publicKeyPem,
        RSAEncryptionPadding? padding = null,
        Encoding? encoding = null) =>
        RsaCryptography.Encrypt(input, publicKeyPem, padding, encoding);

    /// <summary>Decrypts RSA Base64 ciphertext with a private key.</summary>
    public static string DecryptWithRsa(
        this string input,
        string privateKeyPem,
        RSAEncryptionPadding? padding = null,
        Encoding? encoding = null) =>
        RsaCryptography.Decrypt(input, privateKeyPem, padding, encoding);

    /// <summary>Signs a string with an RSA private key and returns a Base64 signature.</summary>
    public static string SignWithRsa(
        this string input,
        string privateKeyPem,
        HashAlgorithmName? hashAlgorithm = null,
        RSASignaturePadding? padding = null,
        Encoding? encoding = null) =>
        RsaCryptography.Sign(input, privateKeyPem, hashAlgorithm, padding, encoding);

    /// <summary>Verifies a Base64 RSA signature for a string.</summary>
    public static bool VerifyRsaSignature(
        this string input,
        string signature,
        string publicKeyPem,
        HashAlgorithmName? hashAlgorithm = null,
        RSASignaturePadding? padding = null,
        Encoding? encoding = null) =>
        RsaCryptography.VerifySignature(input, signature, publicKeyPem, hashAlgorithm, padding, encoding);

    /// <summary>Hashes a password using PBKDF2.</summary>
    public static string HashPasswordWithPbkdf2(
        this string password,
        int iterations = 100000,
        int keyLength = 32,
        int saltLength = 16,
        HashAlgorithmName? hashAlgorithm = null,
        Encoding? encoding = null) =>
        Pbkdf2PasswordHasher.HashPassword(password, iterations, keyLength, saltLength, hashAlgorithm, encoding);

    /// <summary>Verifies a password against a PBKDF2 password hash.</summary>
    public static bool VerifyPbkdf2Password(
        this string password,
        string hashedPassword,
        int iterations = 100000,
        HashAlgorithmName? hashAlgorithm = null,
        Encoding? encoding = null) =>
        Pbkdf2PasswordHasher.VerifyPassword(password, hashedPassword, iterations, hashAlgorithm, encoding);
}
