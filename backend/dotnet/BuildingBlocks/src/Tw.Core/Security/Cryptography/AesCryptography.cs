using System.Security.Cryptography;
using System.Text;
using Tw.Core;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// Provides AES encryption and decryption helpers for strings, bytes, files, and streams.
/// </summary>
public static class AesCryptography
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
        return SymmetricCryptographyCore.EncryptString(
            input,
            key,
            iv,
            mode,
            padding,
            isKeyBase64,
            encoding,
            AesAlgorithm);
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
        return SymmetricCryptographyCore.EncryptBytes(bytes, key, iv, mode, padding, AesAlgorithm);
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
        return SymmetricCryptographyCore.DecryptString(
            input,
            key,
            iv,
            mode,
            padding,
            isKeyBase64,
            encoding,
            AesAlgorithm);
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
        return SymmetricCryptographyCore.DecryptBytes(bytes, key, iv, mode, padding, AesAlgorithm);
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
        return SymmetricCryptographyCore.EncryptFileAsync(filePath, key, iv, mode, padding, AesAlgorithm, cancellationToken);
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
        return SymmetricCryptographyCore.EncryptStreamAsync(stream, key, iv, mode, padding, AesAlgorithm, cancellationToken);
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
        return SymmetricCryptographyCore.DecryptFileAsync(filePath, key, iv, mode, padding, AesAlgorithm, cancellationToken);
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
        return SymmetricCryptographyCore.DecryptStreamAsync(stream, key, iv, mode, padding, AesAlgorithm, cancellationToken);
    }

    private static SymmetricAlgorithmProfile AesAlgorithm => new(
        Aes.Create,
        ValidKeyLengths: [16, 24, 32],
        IvLength: 16,
        KeyLengthMessage: "The key length must be 16, 24, or 32 bytes.",
        IvLengthMessage: "The IV length must be 16 bytes.");
}

internal readonly record struct SymmetricAlgorithmProfile(
    Func<SymmetricAlgorithm> Create,
    int[] ValidKeyLengths,
    int IvLength,
    string KeyLengthMessage,
    string IvLengthMessage);

internal static class SymmetricCryptographyCore
{
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static string EncryptString(
        string input,
        string key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        bool isKeyBase64,
        Encoding? encoding,
        SymmetricAlgorithmProfile profile)
    {
        Check.NotNullOrWhiteSpace(input);
        Check.NotNullOrWhiteSpace(key);

        var textEncoding = encoding ?? DefaultEncoding;
        var keyBytes = isKeyBase64 ? Convert.FromBase64String(key) : textEncoding.GetBytes(key);
        var encrypted = EncryptBytes(textEncoding.GetBytes(input), keyBytes, iv, mode, padding, profile);

        return Convert.ToBase64String(encrypted);
    }

    public static string DecryptString(
        string input,
        string key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        bool isKeyBase64,
        Encoding? encoding,
        SymmetricAlgorithmProfile profile)
    {
        Check.NotNullOrWhiteSpace(input);
        Check.NotNullOrWhiteSpace(key);

        var textEncoding = encoding ?? DefaultEncoding;
        var keyBytes = isKeyBase64 ? Convert.FromBase64String(key) : textEncoding.GetBytes(key);
        var decrypted = DecryptBytes(Convert.FromBase64String(input), keyBytes, iv, mode, padding, profile);

        return textEncoding.GetString(decrypted);
    }

    public static byte[] EncryptBytes(
        byte[] bytes,
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile)
    {
        Check.NotNull(bytes);

        using var algorithm = CreateAlgorithm(key, iv, mode, padding, profile);
        using var encryptor = algorithm.CreateEncryptor();

        var cipherBytes = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return ShouldPrefixIv(mode, iv)
            ? PrefixIv(algorithm.IV, cipherBytes)
            : cipherBytes;
    }

    public static byte[] DecryptBytes(
        byte[] bytes,
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile)
    {
        Check.NotNull(bytes);

        var cipherBytes = bytes;
        if (ShouldPrefixIv(mode, iv))
        {
            (iv, cipherBytes) = SplitPrefixedIv(bytes, profile.IvLength);
        }

        using var algorithm = CreateAlgorithm(key, iv, mode, padding, profile);
        using var decryptor = algorithm.CreateDecryptor();

        return decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
    }

    public static async Task<byte[]> EncryptFileAsync(
        string filePath,
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile,
        CancellationToken cancellationToken)
    {
        Check.NotNullOrWhiteSpace(filePath);

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return await EncryptStreamAsync(stream, key, iv, mode, padding, profile, cancellationToken);
    }

    public static async Task<byte[]> EncryptStreamAsync(
        Stream stream,
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile,
        CancellationToken cancellationToken)
    {
        Check.NotNull(stream);

        using var algorithm = CreateAlgorithm(key, iv, mode, padding, profile);
        await using var output = new MemoryStream();

        if (ShouldPrefixIv(mode, iv))
        {
            await output.WriteAsync(algorithm.IV.AsMemory(), cancellationToken);
        }

        using (var cryptoStream = new CryptoStream(output, algorithm.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
        {
            await stream.CopyToAsync(cryptoStream, cancellationToken);
        }

        return output.ToArray();
    }

    public static async Task<byte[]> DecryptFileAsync(
        string filePath,
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile,
        CancellationToken cancellationToken)
    {
        Check.NotNullOrWhiteSpace(filePath);

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return await DecryptStreamAsync(stream, key, iv, mode, padding, profile, cancellationToken);
    }

    public static async Task<byte[]> DecryptStreamAsync(
        Stream stream,
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile,
        CancellationToken cancellationToken)
    {
        Check.NotNull(stream);

        if (ShouldPrefixIv(mode, iv))
        {
            iv = await ReadPrefixedIvAsync(stream, profile.IvLength, cancellationToken);
        }

        using var algorithm = CreateAlgorithm(key, iv, mode, padding, profile);
        await using var output = new MemoryStream();

        using (var cryptoStream = new CryptoStream(stream, algorithm.CreateDecryptor(), CryptoStreamMode.Read, leaveOpen: true))
        {
            await cryptoStream.CopyToAsync(output, cancellationToken);
        }

        return output.ToArray();
    }

    private static SymmetricAlgorithm CreateAlgorithm(
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile)
    {
        Check.NotNull(key);
        ValidateKeyLength(key, profile);
        ValidateIvLength(iv, mode, profile);

        var algorithm = profile.Create();
        algorithm.Key = key;
        algorithm.Mode = mode;
        algorithm.Padding = padding;

        if (mode != CipherMode.ECB && iv is not null)
        {
            algorithm.IV = iv;
        }

        return algorithm;
    }

    private static bool ShouldPrefixIv(CipherMode mode, byte[]? iv)
    {
        return mode != CipherMode.ECB && iv is null;
    }

    private static void ValidateKeyLength(byte[] key, SymmetricAlgorithmProfile profile)
    {
        if (!profile.ValidKeyLengths.Contains(key.Length))
        {
            throw new ArgumentException(profile.KeyLengthMessage, nameof(key));
        }
    }

    private static void ValidateIvLength(byte[]? iv, CipherMode mode, SymmetricAlgorithmProfile profile)
    {
        if (mode != CipherMode.ECB && iv is not null && iv.Length != profile.IvLength)
        {
            throw new ArgumentException(profile.IvLengthMessage, nameof(iv));
        }
    }

    private static byte[] PrefixIv(byte[] iv, byte[] cipherBytes)
    {
        var result = new byte[iv.Length + cipherBytes.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

        return result;
    }

    private static (byte[] Iv, byte[] CipherBytes) SplitPrefixedIv(byte[] bytes, int ivLength)
    {
        if (bytes.Length < ivLength)
        {
            throw new ArgumentException("Encrypted data is too short to extract the IV.", nameof(bytes));
        }

        var iv = bytes[..ivLength];
        var cipherBytes = bytes[ivLength..];

        return (iv, cipherBytes);
    }

    private static async Task<byte[]> ReadPrefixedIvAsync(
        Stream stream,
        int ivLength,
        CancellationToken cancellationToken)
    {
        var iv = new byte[ivLength];
        var offset = 0;

        while (offset < iv.Length)
        {
            var bytesRead = await stream.ReadAsync(iv.AsMemory(offset), cancellationToken);
            if (bytesRead == 0)
            {
                throw new ArgumentException("Encrypted stream is too short to extract the IV.", nameof(stream));
            }

            offset += bytesRead;
        }

        return iv;
    }
}
