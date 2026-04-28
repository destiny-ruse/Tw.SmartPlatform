using System.Security.Cryptography;
using System.Text;
using Tw.Core;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// 提供 HMAC-MD5 哈希计算与验证辅助方法
/// </summary>
public static class HmacMd5Hasher
{
    /// <summary>
    /// 计算字符串的 HMAC-MD5 哈希
    /// </summary>
    /// <param name="key">HMAC 密钥</param>
    /// <param name="input">要计算哈希的字符串</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="useShortHash">是否返回旧版 MD5 哈希的中间 16 个字符片段</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>十六进制字符串形式的 HMAC-MD5 哈希</returns>
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
    /// 计算字节的 HMAC-MD5 哈希
    /// </summary>
    /// <param name="key">HMAC 密钥字节</param>
    /// <param name="bytes">要计算哈希的字节</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="useShortHash">是否返回旧版 MD5 哈希的中间 16 个字符片段</param>
    /// <returns>十六进制字符串形式的 HMAC-MD5 哈希</returns>
    public static string ComputeHash(byte[] key, byte[] bytes, bool useUpperCase = false, bool useShortHash = false)
    {
        return HmacComputation.ComputeMd5Hash(key, bytes, useUpperCase, useShortHash, HMACMD5.HashData);
    }

    /// <summary>
    /// 计算文件的 HMAC-MD5 哈希
    /// </summary>
    /// <param name="key">HMAC 密钥</param>
    /// <param name="filePath">要读取的文件路径</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="useShortHash">是否返回旧版 MD5 哈希的中间 16 个字符片段</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <param name="cancellationToken">取消文件读取和哈希操作的令牌</param>
    /// <returns>十六进制字符串形式的 HMAC-MD5 哈希</returns>
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
    /// 计算文件的 HMAC-MD5 哈希
    /// </summary>
    /// <param name="key">HMAC 密钥字节</param>
    /// <param name="filePath">要读取的文件路径</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="useShortHash">是否返回旧版 MD5 哈希的中间 16 个字符片段</param>
    /// <param name="cancellationToken">取消文件读取和哈希操作的令牌</param>
    /// <returns>十六进制字符串形式的 HMAC-MD5 哈希</returns>
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
    /// 计算流的 HMAC-MD5 哈希且不释放该流
    /// </summary>
    /// <param name="key">HMAC 密钥</param>
    /// <param name="stream">要从当前位置开始计算哈希的流</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="useShortHash">是否返回旧版 MD5 哈希的中间 16 个字符片段</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <param name="cancellationToken">取消流读取和哈希操作的令牌</param>
    /// <returns>十六进制字符串形式的 HMAC-MD5 哈希</returns>
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
    /// 计算流的 HMAC-MD5 哈希且不释放该流
    /// </summary>
    /// <param name="key">HMAC 密钥字节</param>
    /// <param name="stream">要从当前位置开始计算哈希的流</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="useShortHash">是否返回旧版 MD5 哈希的中间 16 个字符片段</param>
    /// <param name="cancellationToken">取消流读取和哈希操作的令牌</param>
    /// <returns>十六进制字符串形式的 HMAC-MD5 哈希</returns>
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
    /// 使用固定时间字节比较验证字符串的 HMAC-MD5 哈希
    /// </summary>
    /// <param name="key">HMAC 密钥</param>
    /// <param name="input">要计算并验证哈希的字符串</param>
    /// <param name="hash">预期的十六进制哈希</param>
    /// <param name="useShortHash">是否按旧版 MD5 哈希的中间 16 个字符片段进行验证</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>哈希匹配时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool VerifyHash(string key, string input, string hash, bool useShortHash = false, Encoding? encoding = null)
    {
        return HmacComputation.VerifyMd5Hash(key, input, hash, useShortHash, encoding, HMACMD5.HashData);
    }

    /// <summary>
    /// 使用固定时间字节比较验证字节的 HMAC-MD5 哈希
    /// </summary>
    /// <param name="key">HMAC 密钥字节</param>
    /// <param name="bytes">要计算并验证哈希的字节</param>
    /// <param name="hash">预期的十六进制哈希</param>
    /// <param name="useShortHash">是否按旧版 MD5 哈希的中间 16 个字符片段进行验证</param>
    /// <returns>哈希匹配时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
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

        public override long Length => throw new NotSupportedException("前缀流不支持获取长度。");

        public override long Position
        {
            get => throw new NotSupportedException("前缀流不支持获取位置。");
            set => throw new NotSupportedException("前缀流不支持设置位置。");
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
            throw new NotSupportedException("前缀流不支持定位。");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("前缀流不支持设置长度。");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("前缀流不支持写入。");
        }

        protected override void Dispose(bool disposing)
        {
            _prefixOffset = prefix.Length;
            base.Dispose(disposing);
        }
    }
}
