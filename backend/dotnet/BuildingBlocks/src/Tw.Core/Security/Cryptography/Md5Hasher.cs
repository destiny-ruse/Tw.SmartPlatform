using System.Security.Cryptography;
using System.Text;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// Provides MD5 hash computation and verification helpers.
/// </summary>
public static class Md5Hasher
{
    /// <summary>
    /// Computes the MD5 hash for a string.
    /// </summary>
    /// <param name="input">The string to hash.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="useShortHash">Whether to return the legacy 16-character middle segment of the MD5 hash.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns>The MD5 hash as a hexadecimal string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="input"/> is empty or whitespace.</exception>
    public static string ComputeHash(string input, bool useUpperCase = false, bool useShortHash = false, Encoding? encoding = null)
    {
        return HashComputation.ComputeMd5Hash(input, useUpperCase, useShortHash, encoding, MD5.HashData);
    }

    /// <summary>
    /// Computes the MD5 hash for bytes.
    /// </summary>
    /// <param name="bytes">The bytes to hash.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="useShortHash">Whether to return the legacy 16-character middle segment of the MD5 hash.</param>
    /// <returns>The MD5 hash as a hexadecimal string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is <see langword="null"/>.</exception>
    public static string ComputeHash(byte[] bytes, bool useUpperCase = false, bool useShortHash = false)
    {
        return HashComputation.ComputeMd5Hash(bytes, useUpperCase, useShortHash, MD5.HashData);
    }

    /// <summary>
    /// Computes the MD5 hash for a file.
    /// </summary>
    /// <param name="filePath">The file path to read.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="useShortHash">Whether to return the legacy 16-character middle segment of the MD5 hash.</param>
    /// <param name="cancellationToken">The token that cancels the file read and hash operation.</param>
    /// <returns>The MD5 hash as a hexadecimal string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or whitespace.</exception>
    public static Task<string> ComputeFileHashAsync(
        string filePath,
        bool useUpperCase = false,
        bool useShortHash = false,
        CancellationToken cancellationToken = default)
    {
        return HashComputation.ComputeMd5FileHashAsync(filePath, useUpperCase, useShortHash, MD5.HashDataAsync, cancellationToken);
    }

    /// <summary>
    /// Computes the MD5 hash for a stream without disposing it.
    /// </summary>
    /// <param name="stream">The stream to hash from its current position.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="useShortHash">Whether to return the legacy 16-character middle segment of the MD5 hash.</param>
    /// <param name="cancellationToken">The token that cancels the stream read and hash operation.</param>
    /// <returns>The MD5 hash as a hexadecimal string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <see langword="null"/>.</exception>
    public static Task<string> ComputeFileHashAsync(
        Stream stream,
        bool useUpperCase = false,
        bool useShortHash = false,
        CancellationToken cancellationToken = default)
    {
        return HashComputation.ComputeMd5FileHashAsync(stream, useUpperCase, useShortHash, MD5.HashDataAsync, cancellationToken);
    }

    /// <summary>
    /// Verifies an MD5 hash for a string using fixed-time byte comparison.
    /// </summary>
    /// <param name="input">The string to hash and verify.</param>
    /// <param name="hash">The expected hexadecimal hash.</param>
    /// <param name="useShortHash">Whether to verify against the legacy 16-character middle segment of the MD5 hash.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns><see langword="true"/> when the hash matches; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> or <paramref name="hash"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="input"/> is empty or whitespace.</exception>
    public static bool VerifyHash(string input, string hash, bool useShortHash = false, Encoding? encoding = null)
    {
        return HashComputation.VerifyMd5Hash(input, hash, useShortHash, encoding, MD5.HashData);
    }

    /// <summary>
    /// Verifies an MD5 hash for bytes using fixed-time byte comparison.
    /// </summary>
    /// <param name="bytes">The bytes to hash and verify.</param>
    /// <param name="hash">The expected hexadecimal hash.</param>
    /// <param name="useShortHash">Whether to verify against the legacy 16-character middle segment of the MD5 hash.</param>
    /// <returns><see langword="true"/> when the hash matches; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> or <paramref name="hash"/> is <see langword="null"/>.</exception>
    public static bool VerifyHash(byte[] bytes, string hash, bool useShortHash = false)
    {
        return HashComputation.VerifyMd5Hash(bytes, hash, useShortHash, MD5.HashData);
    }
}
