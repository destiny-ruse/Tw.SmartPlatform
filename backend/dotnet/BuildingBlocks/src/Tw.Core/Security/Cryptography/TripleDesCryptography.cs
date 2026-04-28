using System.Security.Cryptography;
using System.Text;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// Provides TripleDES encryption and decryption helpers for strings, bytes, files, and streams.
/// </summary>
public static class TripleDesCryptography
{
    /// <summary>Encrypts a string and returns Base64 ciphertext.</summary>
    /// <param name="input">The plaintext string to encrypt.</param>
    /// <param name="key">The encryption key as text, or Base64 key bytes when <paramref name="isKeyBase64"/> is <see langword="true"/>.</param>
    /// <param name="iv">The initialization vector, or <see langword="null"/> to prefix a generated IV for non-ECB modes.</param>
    /// <param name="mode">The cipher mode.</param>
    /// <param name="padding">The padding mode.</param>
    /// <param name="isKeyBase64">Whether <paramref name="key"/> contains Base64 encoded key bytes.</param>
    /// <param name="encoding">The text encoding, or UTF-8 when omitted.</param>
    /// <returns>The encrypted payload as a Base64 string.</returns>
    /// <exception cref="ArgumentException">Thrown when the key or IV length is invalid.</exception>
    public static string Encrypt(
        string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null)
    {
        return SymmetricCryptographyCore.EncryptString(input, key, iv, mode, padding, isKeyBase64, encoding, TripleDesAlgorithm);
    }

    /// <summary>Encrypts bytes and returns ciphertext bytes.</summary>
    /// <param name="bytes">The plaintext bytes to encrypt.</param>
    /// <param name="key">The encryption key bytes.</param>
    /// <param name="iv">The initialization vector, or <see langword="null"/> to prefix a generated IV for non-ECB modes.</param>
    /// <param name="mode">The cipher mode.</param>
    /// <param name="padding">The padding mode.</param>
    /// <returns>The encrypted bytes, including a prefixed IV when one is generated.</returns>
    /// <exception cref="ArgumentException">Thrown when the key or IV length is invalid.</exception>
    public static byte[] Encrypt(
        byte[] bytes,
        byte[] key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7)
    {
        return SymmetricCryptographyCore.EncryptBytes(bytes, key, iv, mode, padding, TripleDesAlgorithm);
    }

    /// <summary>Decrypts a Base64 ciphertext string.</summary>
    /// <param name="input">The Base64 ciphertext to decrypt.</param>
    /// <param name="key">The encryption key as text, or Base64 key bytes when <paramref name="isKeyBase64"/> is <see langword="true"/>.</param>
    /// <param name="iv">The initialization vector, or <see langword="null"/> to read a prefixed IV for non-ECB modes.</param>
    /// <param name="mode">The cipher mode.</param>
    /// <param name="padding">The padding mode.</param>
    /// <param name="isKeyBase64">Whether <paramref name="key"/> contains Base64 encoded key bytes.</param>
    /// <param name="encoding">The text encoding, or UTF-8 when omitted.</param>
    /// <returns>The decrypted plaintext string.</returns>
    /// <exception cref="ArgumentException">Thrown when the key, IV, or encrypted payload length is invalid.</exception>
    public static string Decrypt(
        string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null)
    {
        return SymmetricCryptographyCore.DecryptString(input, key, iv, mode, padding, isKeyBase64, encoding, TripleDesAlgorithm);
    }

    /// <summary>Decrypts ciphertext bytes.</summary>
    /// <param name="bytes">The ciphertext bytes, including a prefixed IV when one was generated.</param>
    /// <param name="key">The encryption key bytes.</param>
    /// <param name="iv">The initialization vector, or <see langword="null"/> to read a prefixed IV for non-ECB modes.</param>
    /// <param name="mode">The cipher mode.</param>
    /// <param name="padding">The padding mode.</param>
    /// <returns>The decrypted plaintext bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when the key, IV, or encrypted payload length is invalid.</exception>
    public static byte[] Decrypt(
        byte[] bytes,
        byte[] key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7)
    {
        return SymmetricCryptographyCore.DecryptBytes(bytes, key, iv, mode, padding, TripleDesAlgorithm);
    }

    /// <summary>Encrypts a file path and disposes the opened file stream.</summary>
    /// <param name="filePath">The file path to encrypt.</param>
    /// <param name="key">The encryption key bytes.</param>
    /// <param name="iv">The initialization vector, or <see langword="null"/> to prefix a generated IV for non-ECB modes.</param>
    /// <param name="mode">The cipher mode.</param>
    /// <param name="padding">The padding mode.</param>
    /// <param name="cancellationToken">The token that cancels the file read and encryption operation.</param>
    /// <returns>The encrypted bytes, including a prefixed IV when one is generated.</returns>
    /// <exception cref="ArgumentException">Thrown when the key or IV length is invalid.</exception>
    public static Task<byte[]> EncryptFileAsync(
        string filePath,
        byte[] key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        CancellationToken cancellationToken = default)
    {
        return SymmetricCryptographyCore.EncryptFileAsync(filePath, key, iv, mode, padding, TripleDesAlgorithm, cancellationToken);
    }

    /// <summary>Encrypts a stream without disposing the caller-owned stream.</summary>
    /// <param name="stream">The stream to encrypt from its current position.</param>
    /// <param name="key">The encryption key bytes.</param>
    /// <param name="iv">The initialization vector, or <see langword="null"/> to prefix a generated IV for non-ECB modes.</param>
    /// <param name="mode">The cipher mode.</param>
    /// <param name="padding">The padding mode.</param>
    /// <param name="cancellationToken">The token that cancels the stream read and encryption operation.</param>
    /// <returns>The encrypted bytes, including a prefixed IV when one is generated.</returns>
    /// <exception cref="ArgumentException">Thrown when the key or IV length is invalid.</exception>
    public static Task<byte[]> EncryptFileAsync(
        Stream stream,
        byte[] key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        CancellationToken cancellationToken = default)
    {
        return SymmetricCryptographyCore.EncryptStreamAsync(stream, key, iv, mode, padding, TripleDesAlgorithm, cancellationToken);
    }

    /// <summary>Decrypts a file path and disposes the opened file stream.</summary>
    /// <param name="filePath">The encrypted file path to decrypt.</param>
    /// <param name="key">The encryption key bytes.</param>
    /// <param name="iv">The initialization vector, or <see langword="null"/> to read a prefixed IV for non-ECB modes.</param>
    /// <param name="mode">The cipher mode.</param>
    /// <param name="padding">The padding mode.</param>
    /// <param name="cancellationToken">The token that cancels the file read and decryption operation.</param>
    /// <returns>The decrypted plaintext bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when the key, IV, or encrypted payload length is invalid.</exception>
    public static Task<byte[]> DecryptFileAsync(
        string filePath,
        byte[] key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        CancellationToken cancellationToken = default)
    {
        return SymmetricCryptographyCore.DecryptFileAsync(filePath, key, iv, mode, padding, TripleDesAlgorithm, cancellationToken);
    }

    /// <summary>Decrypts a stream without disposing the caller-owned stream.</summary>
    /// <param name="stream">The encrypted stream to decrypt from its current position.</param>
    /// <param name="key">The encryption key bytes.</param>
    /// <param name="iv">The initialization vector, or <see langword="null"/> to read a prefixed IV for non-ECB modes.</param>
    /// <param name="mode">The cipher mode.</param>
    /// <param name="padding">The padding mode.</param>
    /// <param name="cancellationToken">The token that cancels the stream read and decryption operation.</param>
    /// <returns>The decrypted plaintext bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when the key, IV, or encrypted payload length is invalid.</exception>
    public static Task<byte[]> DecryptFileAsync(
        Stream stream,
        byte[] key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        CancellationToken cancellationToken = default)
    {
        return SymmetricCryptographyCore.DecryptStreamAsync(stream, key, iv, mode, padding, TripleDesAlgorithm, cancellationToken);
    }

    private static SymmetricAlgorithmProfile TripleDesAlgorithm => new(
        TripleDES.Create,
        ValidKeyLengths: [16, 24],
        IvLength: 8,
        KeyLengthMessage: "The key length must be 16 or 24 bytes.",
        IvLengthMessage: "The IV length must be 8 bytes.");
}
