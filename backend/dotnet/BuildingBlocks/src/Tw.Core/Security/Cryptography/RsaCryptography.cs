using System.Security.Cryptography;
using System.Text;
using Tw.Core;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// Provides RSA key generation, encryption, decryption, signing, and signature verification helpers.
/// </summary>
/// <remarks>
/// Encryption defaults to OAEP with SHA-256. Signing defaults to SHA-256 with PKCS#1 signature padding.
/// </remarks>
public static class RsaCryptography
{
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly RSAEncryptionPadding DefaultEncryptionPadding = RSAEncryptionPadding.OaepSHA256;
    private static readonly HashAlgorithmName DefaultSignatureHashAlgorithm = HashAlgorithmName.SHA256;
    private static readonly RSASignaturePadding DefaultSignaturePadding = RSASignaturePadding.Pkcs1;

    /// <summary>Generates an RSA key pair encoded as PEM.</summary>
    /// <param name="keySize">The RSA key size in bits.</param>
    /// <returns>The generated RSA key pair.</returns>
    public static RsaKeyPair GenerateKeyPair(int keySize = 2048)
    {
        Check.Positive(keySize);

        using var rsa = RSA.Create(keySize);
        return new RsaKeyPair(rsa.ExportRSAPublicKeyPem(), rsa.ExportRSAPrivateKeyPem());
    }

    /// <summary>Generates an RSA key pair encoded as DER bytes.</summary>
    /// <param name="keySize">The RSA key size in bits.</param>
    /// <returns>The generated RSA key pair.</returns>
    public static RsaDerKeyPair GenerateDerKeyPair(int keySize = 2048)
    {
        Check.Positive(keySize);

        using var rsa = RSA.Create(keySize);
        return new RsaDerKeyPair(rsa.ExportRSAPublicKey(), rsa.ExportRSAPrivateKey());
    }

    /// <summary>Encrypts a string with a PEM public key and returns Base64 ciphertext.</summary>
    /// <param name="input">The text to encrypt.</param>
    /// <param name="publicKeyPem">The public key in PEM format.</param>
    /// <param name="padding">The RSA encryption padding, or OAEP SHA-256 when omitted.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns>The encrypted bytes encoded as Base64.</returns>
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

    /// <summary>Encrypts bytes with a PEM public key.</summary>
    /// <param name="bytes">The bytes to encrypt.</param>
    /// <param name="publicKeyPem">The public key in PEM format.</param>
    /// <param name="padding">The RSA encryption padding, or OAEP SHA-256 when omitted.</param>
    /// <returns>The encrypted bytes.</returns>
    public static byte[] Encrypt(byte[] bytes, string publicKeyPem, RSAEncryptionPadding? padding = null)
    {
        Check.NotNull(bytes);
        Check.NotNullOrWhiteSpace(publicKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        return rsa.Encrypt(bytes, padding ?? DefaultEncryptionPadding);
    }

    /// <summary>Encrypts bytes with a DER public key.</summary>
    /// <param name="bytes">The bytes to encrypt.</param>
    /// <param name="publicKeyDer">The public key in DER format.</param>
    /// <param name="padding">The RSA encryption padding, or OAEP SHA-256 when omitted.</param>
    /// <returns>The encrypted bytes.</returns>
    public static byte[] Encrypt(byte[] bytes, byte[] publicKeyDer, RSAEncryptionPadding? padding = null)
    {
        Check.NotNull(bytes);
        Check.NotNull(publicKeyDer);

        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publicKeyDer, out _);
        return rsa.Encrypt(bytes, padding ?? DefaultEncryptionPadding);
    }

    /// <summary>Decrypts Base64 ciphertext with a PEM private key.</summary>
    /// <param name="input">The Base64 ciphertext to decrypt.</param>
    /// <param name="privateKeyPem">The private key in PEM format.</param>
    /// <param name="padding">The RSA encryption padding, or OAEP SHA-256 when omitted.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns>The decrypted text.</returns>
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

    /// <summary>Decrypts bytes with a PEM private key.</summary>
    /// <param name="bytes">The bytes to decrypt.</param>
    /// <param name="privateKeyPem">The private key in PEM format.</param>
    /// <param name="padding">The RSA encryption padding, or OAEP SHA-256 when omitted.</param>
    /// <returns>The decrypted bytes.</returns>
    public static byte[] Decrypt(byte[] bytes, string privateKeyPem, RSAEncryptionPadding? padding = null)
    {
        Check.NotNull(bytes);
        Check.NotNullOrWhiteSpace(privateKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        return rsa.Decrypt(bytes, padding ?? DefaultEncryptionPadding);
    }

    /// <summary>Decrypts bytes with a DER private key.</summary>
    /// <param name="bytes">The bytes to decrypt.</param>
    /// <param name="privateKeyDer">The private key in DER format.</param>
    /// <param name="padding">The RSA encryption padding, or OAEP SHA-256 when omitted.</param>
    /// <returns>The decrypted bytes.</returns>
    public static byte[] Decrypt(byte[] bytes, byte[] privateKeyDer, RSAEncryptionPadding? padding = null)
    {
        Check.NotNull(bytes);
        Check.NotNull(privateKeyDer);

        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKeyDer, out _);
        return rsa.Decrypt(bytes, padding ?? DefaultEncryptionPadding);
    }

    /// <summary>Signs a string with a PEM private key and returns a Base64 signature.</summary>
    /// <param name="input">The text to sign.</param>
    /// <param name="privateKeyPem">The private key in PEM format.</param>
    /// <param name="hashAlgorithm">The signature hash algorithm, or SHA-256 when omitted.</param>
    /// <param name="padding">The RSA signature padding, or PKCS#1 when omitted.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns>The signature encoded as Base64.</returns>
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

    /// <summary>Signs bytes with a PEM private key.</summary>
    /// <param name="bytes">The bytes to sign.</param>
    /// <param name="privateKeyPem">The private key in PEM format.</param>
    /// <param name="hashAlgorithm">The signature hash algorithm, or SHA-256 when omitted.</param>
    /// <param name="padding">The RSA signature padding, or PKCS#1 when omitted.</param>
    /// <returns>The signature bytes.</returns>
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

    /// <summary>Verifies a Base64 signature for a string with a PEM public key.</summary>
    /// <param name="input">The text whose signature is verified.</param>
    /// <param name="signature">The Base64 signature.</param>
    /// <param name="publicKeyPem">The public key in PEM format.</param>
    /// <param name="hashAlgorithm">The signature hash algorithm, or SHA-256 when omitted.</param>
    /// <param name="padding">The RSA signature padding, or PKCS#1 when omitted.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns><see langword="true"/> when the signature is valid; otherwise, <see langword="false"/>.</returns>
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

    /// <summary>Verifies a signature for bytes with a PEM public key.</summary>
    /// <param name="bytes">The bytes whose signature is verified.</param>
    /// <param name="signature">The signature bytes.</param>
    /// <param name="publicKeyPem">The public key in PEM format.</param>
    /// <param name="hashAlgorithm">The signature hash algorithm, or SHA-256 when omitted.</param>
    /// <param name="padding">The RSA signature padding, or PKCS#1 when omitted.</param>
    /// <returns><see langword="true"/> when the signature is valid; otherwise, <see langword="false"/>.</returns>
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
