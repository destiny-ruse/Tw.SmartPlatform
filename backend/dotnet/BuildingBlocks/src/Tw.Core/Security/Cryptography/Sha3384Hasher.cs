using System.Text;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// Provides SHA3-384 hash computation and verification helpers.
/// </summary>
public static class Sha3384Hasher
{
    /// <summary>Computes the SHA3-384 hash for a string.</summary>
    /// <param name="input">The string to hash.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns>The SHA3-384 hash as a hexadecimal string.</returns>
    public static string ComputeHash(string input, bool useUpperCase = false, Encoding? encoding = null)
    {
        return HashComputation.ComputeHash(input, useUpperCase, encoding, Sha3Hash.Hash384);
    }

    /// <summary>Computes the SHA3-384 hash for bytes.</summary>
    /// <param name="bytes">The bytes to hash.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <returns>The SHA3-384 hash as a hexadecimal string.</returns>
    public static string ComputeHash(byte[] bytes, bool useUpperCase = false)
    {
        return HashComputation.ComputeHash(bytes, useUpperCase, Sha3Hash.Hash384);
    }

    /// <summary>Computes the SHA3-384 hash for a file.</summary>
    /// <param name="filePath">The file path to read.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="cancellationToken">The token that cancels the file read and hash operation.</param>
    /// <returns>The SHA3-384 hash as a hexadecimal string.</returns>
    public static Task<string> ComputeFileHashAsync(string filePath, bool useUpperCase = false, CancellationToken cancellationToken = default)
    {
        return HashComputation.ComputeFileHashAsync(filePath, useUpperCase, Sha3Hash.Hash384Async, cancellationToken);
    }

    /// <summary>Computes the SHA3-384 hash for a stream without disposing it.</summary>
    /// <param name="stream">The stream to hash from its current position.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="cancellationToken">The token that cancels the stream read and hash operation.</param>
    /// <returns>The SHA3-384 hash as a hexadecimal string.</returns>
    public static Task<string> ComputeFileHashAsync(Stream stream, bool useUpperCase = false, CancellationToken cancellationToken = default)
    {
        return HashComputation.ComputeFileHashAsync(stream, useUpperCase, Sha3Hash.Hash384Async, cancellationToken);
    }

    /// <summary>Verifies a SHA3-384 hash for a string using fixed-time byte comparison.</summary>
    /// <param name="input">The string to hash and verify.</param>
    /// <param name="hash">The expected hexadecimal hash.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns><see langword="true"/> when the hash matches; otherwise, <see langword="false"/>.</returns>
    public static bool VerifyHash(string input, string hash, Encoding? encoding = null)
    {
        return HashComputation.VerifyHash(input, hash, encoding, Sha3Hash.Hash384);
    }

    /// <summary>Verifies a SHA3-384 hash for bytes using fixed-time byte comparison.</summary>
    /// <param name="bytes">The bytes to hash and verify.</param>
    /// <param name="hash">The expected hexadecimal hash.</param>
    /// <returns><see langword="true"/> when the hash matches; otherwise, <see langword="false"/>.</returns>
    public static bool VerifyHash(byte[] bytes, string hash)
    {
        return HashComputation.VerifyHash(bytes, hash, Sha3Hash.Hash384);
    }
}
