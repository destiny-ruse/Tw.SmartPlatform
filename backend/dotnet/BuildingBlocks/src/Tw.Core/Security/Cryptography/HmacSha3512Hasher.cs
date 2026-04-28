using System.Text;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// Provides HMAC-SHA3-512 hash computation and verification helpers.
/// </summary>
public static class HmacSha3512Hasher
{
    /// <summary>Computes the HMAC-SHA3-512 hash for a string.</summary>
    /// <param name="key">The HMAC key.</param>
    /// <param name="input">The string to hash.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns>The HMAC-SHA3-512 hash as a hexadecimal string.</returns>
    public static string ComputeHash(string key, string input, bool useUpperCase = false, Encoding? encoding = null)
    {
        return HmacComputation.ComputeHash(key, input, useUpperCase, encoding, HmacSha3Hash.Hash512);
    }

    /// <summary>Computes the HMAC-SHA3-512 hash for bytes.</summary>
    /// <param name="key">The HMAC key bytes.</param>
    /// <param name="bytes">The bytes to hash.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <returns>The HMAC-SHA3-512 hash as a hexadecimal string.</returns>
    public static string ComputeHash(byte[] key, byte[] bytes, bool useUpperCase = false)
    {
        return HmacComputation.ComputeHash(key, bytes, useUpperCase, HmacSha3Hash.Hash512);
    }

    /// <summary>Computes the HMAC-SHA3-512 hash for a file.</summary>
    /// <param name="key">The HMAC key.</param>
    /// <param name="filePath">The file path to read.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <param name="cancellationToken">The token that cancels the file read and hash operation.</param>
    /// <returns>The HMAC-SHA3-512 hash as a hexadecimal string.</returns>
    public static Task<string> ComputeFileHashAsync(
        string key,
        string filePath,
        bool useUpperCase = false,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        return HmacComputation.ComputeFileHashAsync(key, filePath, useUpperCase, encoding, HmacSha3Hash.Hash512Async, cancellationToken);
    }

    /// <summary>Computes the HMAC-SHA3-512 hash for a file.</summary>
    /// <param name="key">The HMAC key bytes.</param>
    /// <param name="filePath">The file path to read.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="cancellationToken">The token that cancels the file read and hash operation.</param>
    /// <returns>The HMAC-SHA3-512 hash as a hexadecimal string.</returns>
    public static Task<string> ComputeFileHashAsync(
        byte[] key,
        string filePath,
        bool useUpperCase = false,
        CancellationToken cancellationToken = default)
    {
        return HmacComputation.ComputeFileHashAsync(key, filePath, useUpperCase, HmacSha3Hash.Hash512Async, cancellationToken);
    }

    /// <summary>Computes the HMAC-SHA3-512 hash for a stream without disposing it.</summary>
    /// <param name="key">The HMAC key.</param>
    /// <param name="stream">The stream to hash from its current position.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <param name="cancellationToken">The token that cancels the stream read and hash operation.</param>
    /// <returns>The HMAC-SHA3-512 hash as a hexadecimal string.</returns>
    public static Task<string> ComputeFileHashAsync(
        string key,
        Stream stream,
        bool useUpperCase = false,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        return HmacComputation.ComputeFileHashAsync(key, stream, useUpperCase, encoding, HmacSha3Hash.Hash512Async, cancellationToken);
    }

    /// <summary>Computes the HMAC-SHA3-512 hash for a stream without disposing it.</summary>
    /// <param name="key">The HMAC key bytes.</param>
    /// <param name="stream">The stream to hash from its current position.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="cancellationToken">The token that cancels the stream read and hash operation.</param>
    /// <returns>The HMAC-SHA3-512 hash as a hexadecimal string.</returns>
    public static Task<string> ComputeFileHashAsync(
        byte[] key,
        Stream stream,
        bool useUpperCase = false,
        CancellationToken cancellationToken = default)
    {
        return HmacComputation.ComputeFileHashAsync(key, stream, useUpperCase, HmacSha3Hash.Hash512Async, cancellationToken);
    }

    /// <summary>Verifies an HMAC-SHA3-512 hash for a string using fixed-time byte comparison.</summary>
    /// <param name="key">The HMAC key.</param>
    /// <param name="input">The string to hash and verify.</param>
    /// <param name="hash">The expected hexadecimal hash.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns><see langword="true"/> when the hash matches; otherwise, <see langword="false"/>.</returns>
    public static bool VerifyHash(string key, string input, string hash, Encoding? encoding = null)
    {
        return HmacComputation.VerifyHash(key, input, hash, encoding, HmacSha3Hash.Hash512);
    }

    /// <summary>Verifies an HMAC-SHA3-512 hash for bytes using fixed-time byte comparison.</summary>
    /// <param name="key">The HMAC key bytes.</param>
    /// <param name="bytes">The bytes to hash and verify.</param>
    /// <param name="hash">The expected hexadecimal hash.</param>
    /// <returns><see langword="true"/> when the hash matches; otherwise, <see langword="false"/>.</returns>
    public static bool VerifyHash(byte[] key, byte[] bytes, string hash)
    {
        return HmacComputation.VerifyHash(key, bytes, hash, HmacSha3Hash.Hash512);
    }
}
