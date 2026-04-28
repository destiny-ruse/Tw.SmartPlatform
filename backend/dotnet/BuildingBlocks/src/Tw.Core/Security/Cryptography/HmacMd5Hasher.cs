using System.Security.Cryptography;
using System.Text;
using Tw.Core;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// Provides HMAC-MD5 hash computation and verification helpers.
/// </summary>
public static class HmacMd5Hasher
{
    /// <summary>
    /// Computes the HMAC-MD5 hash for a string.
    /// </summary>
    /// <param name="key">The HMAC key.</param>
    /// <param name="input">The string to hash.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="useShortHash">Whether to return the legacy 16-character middle segment of the MD5 hash.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns>The HMAC-MD5 hash as a hexadecimal string.</returns>
    public static string ComputeHash(
        string key,
        string input,
        bool useUpperCase = false,
        bool useShortHash = false,
        Encoding? encoding = null)
    {
        return HmacComputation.ComputeMd5Hash(key, input, useUpperCase, useShortHash, encoding, HMACMD5.HashData);
    }

    /// <summary>
    /// Computes the HMAC-MD5 hash for bytes.
    /// </summary>
    /// <param name="key">The HMAC key bytes.</param>
    /// <param name="bytes">The bytes to hash.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="useShortHash">Whether to return the legacy 16-character middle segment of the MD5 hash.</param>
    /// <returns>The HMAC-MD5 hash as a hexadecimal string.</returns>
    public static string ComputeHash(byte[] key, byte[] bytes, bool useUpperCase = false, bool useShortHash = false)
    {
        return HmacComputation.ComputeMd5Hash(key, bytes, useUpperCase, useShortHash, HMACMD5.HashData);
    }

    /// <summary>
    /// Computes the HMAC-MD5 hash for a file.
    /// </summary>
    /// <param name="key">The HMAC key.</param>
    /// <param name="filePath">The file path to read.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="useShortHash">Whether to return the legacy 16-character middle segment of the MD5 hash.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <param name="cancellationToken">The token that cancels the file read and hash operation.</param>
    /// <returns>The HMAC-MD5 hash as a hexadecimal string.</returns>
    public static Task<string> ComputeFileHashAsync(
        string key,
        string filePath,
        bool useUpperCase = false,
        bool useShortHash = false,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        return HmacComputation.ComputeMd5FileHashAsync(
            key,
            filePath,
            useUpperCase,
            useShortHash,
            encoding,
            HMACMD5.HashDataAsync,
            cancellationToken);
    }

    /// <summary>
    /// Computes the HMAC-MD5 hash for a file.
    /// </summary>
    /// <param name="key">The HMAC key bytes.</param>
    /// <param name="filePath">The file path to read.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="useShortHash">Whether to return the legacy 16-character middle segment of the MD5 hash.</param>
    /// <param name="cancellationToken">The token that cancels the file read and hash operation.</param>
    /// <returns>The HMAC-MD5 hash as a hexadecimal string.</returns>
    public static Task<string> ComputeFileHashAsync(
        byte[] key,
        string filePath,
        bool useUpperCase = false,
        bool useShortHash = false,
        CancellationToken cancellationToken = default)
    {
        return HmacComputation.ComputeMd5FileHashAsync(
            key,
            filePath,
            useUpperCase,
            useShortHash,
            HMACMD5.HashDataAsync,
            cancellationToken);
    }

    /// <summary>
    /// Computes the HMAC-MD5 hash for a stream without disposing it.
    /// </summary>
    /// <param name="key">The HMAC key.</param>
    /// <param name="stream">The stream to hash from its current position.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="useShortHash">Whether to return the legacy 16-character middle segment of the MD5 hash.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <param name="cancellationToken">The token that cancels the stream read and hash operation.</param>
    /// <returns>The HMAC-MD5 hash as a hexadecimal string.</returns>
    public static Task<string> ComputeFileHashAsync(
        string key,
        Stream stream,
        bool useUpperCase = false,
        bool useShortHash = false,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        return HmacComputation.ComputeMd5FileHashAsync(
            key,
            stream,
            useUpperCase,
            useShortHash,
            encoding,
            HMACMD5.HashDataAsync,
            cancellationToken);
    }

    /// <summary>
    /// Computes the HMAC-MD5 hash for a stream without disposing it.
    /// </summary>
    /// <param name="key">The HMAC key bytes.</param>
    /// <param name="stream">The stream to hash from its current position.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="useShortHash">Whether to return the legacy 16-character middle segment of the MD5 hash.</param>
    /// <param name="cancellationToken">The token that cancels the stream read and hash operation.</param>
    /// <returns>The HMAC-MD5 hash as a hexadecimal string.</returns>
    public static Task<string> ComputeFileHashAsync(
        byte[] key,
        Stream stream,
        bool useUpperCase = false,
        bool useShortHash = false,
        CancellationToken cancellationToken = default)
    {
        return HmacComputation.ComputeMd5FileHashAsync(
            key,
            stream,
            useUpperCase,
            useShortHash,
            HMACMD5.HashDataAsync,
            cancellationToken);
    }

    /// <summary>
    /// Verifies an HMAC-MD5 hash for a string using fixed-time byte comparison.
    /// </summary>
    /// <param name="key">The HMAC key.</param>
    /// <param name="input">The string to hash and verify.</param>
    /// <param name="hash">The expected hexadecimal hash.</param>
    /// <param name="useShortHash">Whether to verify against the legacy 16-character middle segment of the MD5 hash.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns><see langword="true"/> when the hash matches; otherwise, <see langword="false"/>.</returns>
    public static bool VerifyHash(string key, string input, string hash, bool useShortHash = false, Encoding? encoding = null)
    {
        return HmacComputation.VerifyMd5Hash(key, input, hash, useShortHash, encoding, HMACMD5.HashData);
    }

    /// <summary>
    /// Verifies an HMAC-MD5 hash for bytes using fixed-time byte comparison.
    /// </summary>
    /// <param name="key">The HMAC key bytes.</param>
    /// <param name="bytes">The bytes to hash and verify.</param>
    /// <param name="hash">The expected hexadecimal hash.</param>
    /// <param name="useShortHash">Whether to verify against the legacy 16-character middle segment of the MD5 hash.</param>
    /// <returns><see langword="true"/> when the hash matches; otherwise, <see langword="false"/>.</returns>
    public static bool VerifyHash(byte[] key, byte[] bytes, string hash, bool useShortHash = false)
    {
        return HmacComputation.VerifyMd5Hash(key, bytes, hash, useShortHash, HMACMD5.HashData);
    }
}

internal static class HmacComputation
{
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static string ComputeHash(
        string key,
        string input,
        bool useUpperCase,
        Encoding? encoding,
        Func<byte[], byte[], byte[]> computeHash)
    {
        Check.NotNullOrWhiteSpace(key);
        Check.NotNullOrWhiteSpace(input);

        var effectiveEncoding = encoding ?? DefaultEncoding;
        return ComputeHash(effectiveEncoding.GetBytes(key), effectiveEncoding.GetBytes(input), useUpperCase, computeHash);
    }

    public static string ComputeHash(byte[] key, byte[] bytes, bool useUpperCase, Func<byte[], byte[], byte[]> computeHash)
    {
        Check.NotNull(key);
        Check.NotNull(bytes);
        Check.NotNull(computeHash);

        return HexEncoding.ToHex(computeHash(key, bytes), useUpperCase);
    }

    public static string ComputeMd5Hash(
        string key,
        string input,
        bool useUpperCase,
        bool useShortHash,
        Encoding? encoding,
        Func<byte[], byte[], byte[]> computeHash)
    {
        Check.NotNullOrWhiteSpace(key);
        Check.NotNullOrWhiteSpace(input);

        var effectiveEncoding = encoding ?? DefaultEncoding;
        return ComputeMd5Hash(effectiveEncoding.GetBytes(key), effectiveEncoding.GetBytes(input), useUpperCase, useShortHash, computeHash);
    }

    public static string ComputeMd5Hash(
        byte[] key,
        byte[] bytes,
        bool useUpperCase,
        bool useShortHash,
        Func<byte[], byte[], byte[]> computeHash)
    {
        Check.NotNull(key);
        Check.NotNull(bytes);
        Check.NotNull(computeHash);

        return FormatMd5Hash(computeHash(key, bytes), useUpperCase, useShortHash);
    }

    public static async Task<string> ComputeFileHashAsync(
        string key,
        string filePath,
        bool useUpperCase,
        Encoding? encoding,
        Func<byte[], Stream, CancellationToken, ValueTask<byte[]>> computeHashAsync,
        CancellationToken cancellationToken)
    {
        Check.NotNullOrWhiteSpace(key);

        var effectiveEncoding = encoding ?? DefaultEncoding;
        return await ComputeFileHashAsync(
            effectiveEncoding.GetBytes(key),
            filePath,
            useUpperCase,
            computeHashAsync,
            cancellationToken);
    }

    public static async Task<string> ComputeFileHashAsync(
        byte[] key,
        string filePath,
        bool useUpperCase,
        Func<byte[], Stream, CancellationToken, ValueTask<byte[]>> computeHashAsync,
        CancellationToken cancellationToken)
    {
        Check.NotNull(key);
        Check.NotNullOrWhiteSpace(filePath);

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return await ComputeFileHashAsync(key, stream, useUpperCase, computeHashAsync, cancellationToken);
    }

    public static async Task<string> ComputeFileHashAsync(
        string key,
        Stream stream,
        bool useUpperCase,
        Encoding? encoding,
        Func<byte[], Stream, CancellationToken, ValueTask<byte[]>> computeHashAsync,
        CancellationToken cancellationToken)
    {
        Check.NotNullOrWhiteSpace(key);

        var effectiveEncoding = encoding ?? DefaultEncoding;
        return await ComputeFileHashAsync(
            effectiveEncoding.GetBytes(key),
            stream,
            useUpperCase,
            computeHashAsync,
            cancellationToken);
    }

    public static async Task<string> ComputeFileHashAsync(
        byte[] key,
        Stream stream,
        bool useUpperCase,
        Func<byte[], Stream, CancellationToken, ValueTask<byte[]>> computeHashAsync,
        CancellationToken cancellationToken)
    {
        Check.NotNull(key);
        Check.NotNull(stream);
        Check.NotNull(computeHashAsync);

        var hash = await computeHashAsync(key, stream, cancellationToken);
        return HexEncoding.ToHex(hash, useUpperCase);
    }

    public static async Task<string> ComputeMd5FileHashAsync(
        string key,
        string filePath,
        bool useUpperCase,
        bool useShortHash,
        Encoding? encoding,
        Func<byte[], Stream, CancellationToken, ValueTask<byte[]>> computeHashAsync,
        CancellationToken cancellationToken)
    {
        Check.NotNullOrWhiteSpace(key);

        var effectiveEncoding = encoding ?? DefaultEncoding;
        return await ComputeMd5FileHashAsync(
            effectiveEncoding.GetBytes(key),
            filePath,
            useUpperCase,
            useShortHash,
            computeHashAsync,
            cancellationToken);
    }

    public static async Task<string> ComputeMd5FileHashAsync(
        byte[] key,
        string filePath,
        bool useUpperCase,
        bool useShortHash,
        Func<byte[], Stream, CancellationToken, ValueTask<byte[]>> computeHashAsync,
        CancellationToken cancellationToken)
    {
        Check.NotNull(key);
        Check.NotNullOrWhiteSpace(filePath);

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return await ComputeMd5FileHashAsync(key, stream, useUpperCase, useShortHash, computeHashAsync, cancellationToken);
    }

    public static async Task<string> ComputeMd5FileHashAsync(
        string key,
        Stream stream,
        bool useUpperCase,
        bool useShortHash,
        Encoding? encoding,
        Func<byte[], Stream, CancellationToken, ValueTask<byte[]>> computeHashAsync,
        CancellationToken cancellationToken)
    {
        Check.NotNullOrWhiteSpace(key);

        var effectiveEncoding = encoding ?? DefaultEncoding;
        return await ComputeMd5FileHashAsync(
            effectiveEncoding.GetBytes(key),
            stream,
            useUpperCase,
            useShortHash,
            computeHashAsync,
            cancellationToken);
    }

    public static async Task<string> ComputeMd5FileHashAsync(
        byte[] key,
        Stream stream,
        bool useUpperCase,
        bool useShortHash,
        Func<byte[], Stream, CancellationToken, ValueTask<byte[]>> computeHashAsync,
        CancellationToken cancellationToken)
    {
        Check.NotNull(key);
        Check.NotNull(stream);
        Check.NotNull(computeHashAsync);

        var hash = await computeHashAsync(key, stream, cancellationToken);
        return FormatMd5Hash(hash, useUpperCase, useShortHash);
    }

    public static bool VerifyHash(
        string key,
        string input,
        string hash,
        Encoding? encoding,
        Func<byte[], byte[], byte[]> computeHash)
    {
        Check.NotNull(hash);

        var computedHash = ComputeHash(key, input, useUpperCase: false, encoding, computeHash);
        return HashComparison.FixedTimeEqualsHex(computedHash, hash);
    }

    public static bool VerifyHash(byte[] key, byte[] bytes, string hash, Func<byte[], byte[], byte[]> computeHash)
    {
        Check.NotNull(hash);

        var computedHash = ComputeHash(key, bytes, useUpperCase: false, computeHash);
        return HashComparison.FixedTimeEqualsHex(computedHash, hash);
    }

    public static bool VerifyMd5Hash(
        string key,
        string input,
        string hash,
        bool useShortHash,
        Encoding? encoding,
        Func<byte[], byte[], byte[]> computeHash)
    {
        Check.NotNull(hash);

        var computedHash = ComputeMd5Hash(key, input, useUpperCase: false, useShortHash, encoding, computeHash);
        return HashComparison.FixedTimeEqualsHex(computedHash, hash);
    }

    public static bool VerifyMd5Hash(
        byte[] key,
        byte[] bytes,
        string hash,
        bool useShortHash,
        Func<byte[], byte[], byte[]> computeHash)
    {
        Check.NotNull(hash);

        var computedHash = ComputeMd5Hash(key, bytes, useUpperCase: false, useShortHash, computeHash);
        return HashComparison.FixedTimeEqualsHex(computedHash, hash);
    }

    private static string FormatMd5Hash(byte[] hash, bool useUpperCase, bool useShortHash)
    {
        var hashString = HexEncoding.ToHex(hash, useUpperCase);
        return useShortHash ? hashString.Substring(8, 16) : hashString;
    }
}

internal static class HmacSha3Hash
{
    public static byte[] Hash256(byte[] key, byte[] bytes)
    {
        return HMACSHA3_256.IsSupported ? HMACSHA3_256.HashData(key, bytes) : ComputeHmac(key, bytes, Sha3Hash.Hash256, rateBytes: 136);
    }

    public static byte[] Hash384(byte[] key, byte[] bytes)
    {
        return HMACSHA3_384.IsSupported ? HMACSHA3_384.HashData(key, bytes) : ComputeHmac(key, bytes, Sha3Hash.Hash384, rateBytes: 104);
    }

    public static byte[] Hash512(byte[] key, byte[] bytes)
    {
        return HMACSHA3_512.IsSupported ? HMACSHA3_512.HashData(key, bytes) : ComputeHmac(key, bytes, Sha3Hash.Hash512, rateBytes: 72);
    }

    public static async ValueTask<byte[]> Hash256Async(byte[] key, Stream stream, CancellationToken cancellationToken)
    {
        if (HMACSHA3_256.IsSupported)
        {
            return await HMACSHA3_256.HashDataAsync(key, stream, cancellationToken);
        }

        return await ComputeHmacAsync(key, stream, Sha3Hash.Hash256, Sha3Hash.Hash256Async, rateBytes: 136, cancellationToken);
    }

    public static async ValueTask<byte[]> Hash384Async(byte[] key, Stream stream, CancellationToken cancellationToken)
    {
        if (HMACSHA3_384.IsSupported)
        {
            return await HMACSHA3_384.HashDataAsync(key, stream, cancellationToken);
        }

        return await ComputeHmacAsync(key, stream, Sha3Hash.Hash384, Sha3Hash.Hash384Async, rateBytes: 104, cancellationToken);
    }

    public static async ValueTask<byte[]> Hash512Async(byte[] key, Stream stream, CancellationToken cancellationToken)
    {
        if (HMACSHA3_512.IsSupported)
        {
            return await HMACSHA3_512.HashDataAsync(key, stream, cancellationToken);
        }

        return await ComputeHmacAsync(key, stream, Sha3Hash.Hash512, Sha3Hash.Hash512Async, rateBytes: 72, cancellationToken);
    }

    private static byte[] ComputeHmac(byte[] key, byte[] bytes, Func<byte[], byte[]> hash, int rateBytes)
    {
        Check.NotNull(key);
        Check.NotNull(bytes);
        Check.NotNull(hash);

        var keyBlock = CreateKeyBlock(key, hash, rateBytes);
        var innerPad = CreatePad(keyBlock, 0x36);
        var outerPad = CreatePad(keyBlock, 0x5c);
        var innerHash = hash(Concat(innerPad, bytes));

        return hash(Concat(outerPad, innerHash));
    }

    private static async ValueTask<byte[]> ComputeHmacAsync(
        byte[] key,
        Stream stream,
        Func<byte[], byte[]> hash,
        Func<Stream, CancellationToken, ValueTask<byte[]>> hashAsync,
        int rateBytes,
        CancellationToken cancellationToken)
    {
        Check.NotNull(key);
        Check.NotNull(stream);
        Check.NotNull(hash);
        Check.NotNull(hashAsync);

        var keyBlock = CreateKeyBlock(key, hash, rateBytes);
        var innerPad = CreatePad(keyBlock, 0x36);
        var outerPad = CreatePad(keyBlock, 0x5c);
        await using var innerStream = new PrefixStream(innerPad, stream);
        var innerHash = await hashAsync(innerStream, cancellationToken);

        return hash(Concat(outerPad, innerHash));
    }

    private static byte[] CreateKeyBlock(byte[] key, Func<byte[], byte[]> hash, int rateBytes)
    {
        var normalizedKey = key.Length > rateBytes ? hash(key) : key;
        var keyBlock = new byte[rateBytes];
        normalizedKey.AsSpan().CopyTo(keyBlock);

        return keyBlock;
    }

    private static byte[] CreatePad(byte[] keyBlock, byte value)
    {
        var pad = new byte[keyBlock.Length];

        for (var index = 0; index < pad.Length; index++)
        {
            pad[index] = (byte)(keyBlock[index] ^ value);
        }

        return pad;
    }

    private static byte[] Concat(byte[] prefix, byte[] suffix)
    {
        var bytes = new byte[prefix.Length + suffix.Length];
        prefix.AsSpan().CopyTo(bytes);
        suffix.AsSpan().CopyTo(bytes.AsSpan(prefix.Length));

        return bytes;
    }

    private sealed class PrefixStream(byte[] prefix, Stream innerStream) : Stream
    {
        private int _prefixOffset;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            if (_prefixOffset < prefix.Length)
            {
                var bytesToCopy = Math.Min(buffer.Length, prefix.Length - _prefixOffset);
                prefix.AsSpan(_prefixOffset, bytesToCopy).CopyTo(buffer);
                _prefixOffset += bytesToCopy;

                return bytesToCopy;
            }

            return innerStream.Read(buffer);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_prefixOffset < prefix.Length)
            {
                var bytesToCopy = Math.Min(buffer.Length, prefix.Length - _prefixOffset);
                prefix.AsMemory(_prefixOffset, bytesToCopy).CopyTo(buffer);
                _prefixOffset += bytesToCopy;

                return bytesToCopy;
            }

            return await innerStream.ReadAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            _prefixOffset = prefix.Length;
            base.Dispose(disposing);
        }
    }
}
