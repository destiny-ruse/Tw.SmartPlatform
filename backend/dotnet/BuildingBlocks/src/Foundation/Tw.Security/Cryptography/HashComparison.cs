using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Tw.Core;

namespace Tw.Security.Cryptography;

/// <summary>
/// 封装HashComparison相关的数据和行为
/// </summary>
internal static class HashComparison
{
    /// <summary>
    /// 说明FixedTimeEqualsHex在当前类型中的职责
    /// </summary>
    /// <param name="expectedHash">用于提供expectedHash</param>
    /// <param name="actualHash">用于提供actualHash</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static bool FixedTimeEqualsHex(string expectedHash, string actualHash)
    {
        Check.NotNull(expectedHash);
        Check.NotNull(actualHash);

        byte[] expectedBytes;
        byte[] actualBytes;

        try
        {
            expectedBytes = HexEncoding.FromHex(expectedHash);
            actualBytes = HexEncoding.FromHex(actualHash);
        }
        catch (FormatException)
        {
            return false;
        }

        return expectedBytes.Length == actualBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}

/// <summary>
/// 封装HashComputation相关的数据和行为
/// </summary>
internal static class HashComputation
{
    /// <summary>
    /// 保存当前类型处理流程依赖的默认Encoding
    /// </summary>
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// 说明ComputeHash在当前类型中的职责
    /// </summary>
    /// <param name="input">用于提供nput</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="encoding">用于提供encoding</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>方法计算得到的文本值</returns>
    public static string ComputeHash(
        string input,
        bool useUpperCase,
        Encoding? encoding,
        Func<byte[], byte[]> computeHash)
    {
        Check.NotNullOrWhiteSpace(input);

        return ComputeHash((encoding ?? DefaultEncoding).GetBytes(input), useUpperCase, computeHash);
    }

    /// <summary>
    /// 说明ComputeHash在当前类型中的职责
    /// </summary>
    /// <param name="bytes">用于提供bytes</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static string ComputeHash(byte[] bytes, bool useUpperCase, Func<byte[], byte[]> computeHash)
    {
        Check.NotNull(bytes);
        Check.NotNull(computeHash);

        return HexEncoding.ToHex(computeHash(bytes), useUpperCase);
    }

    /// <summary>
    /// 说明ComputeMd5Hash在当前类型中的职责
    /// </summary>
    /// <param name="input">用于提供nput</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="encoding">用于提供encoding</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>方法计算得到的文本值</returns>
    public static string ComputeMd5Hash(
        string input,
        bool useUpperCase,
        bool useShortHash,
        Encoding? encoding,
        Func<byte[], byte[]> computeHash)
    {
        Check.NotNullOrWhiteSpace(input);

        return ComputeMd5Hash((encoding ?? DefaultEncoding).GetBytes(input), useUpperCase, useShortHash, computeHash);
    }

    /// <summary>
    /// 说明ComputeMd5Hash在当前类型中的职责
    /// </summary>
    /// <param name="bytes">用于提供bytes</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>方法计算得到的文本值</returns>
    public static string ComputeMd5Hash(
        byte[] bytes,
        bool useUpperCase,
        bool useShortHash,
        Func<byte[], byte[]> computeHash)
    {
        Check.NotNull(bytes);
        Check.NotNull(computeHash);

        return FormatMd5Hash(computeHash(bytes), useUpperCase, useShortHash);
    }

    /// <summary>
    /// 说明ComputeFileHashAsync在当前类型中的职责
    /// </summary>
    /// <param name="filePath">用于提供filePath</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="computeHashAsync">用于提供computeHashAsync</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
    public static async Task<string> ComputeFileHashAsync(
        string filePath,
        bool useUpperCase,
        Func<Stream, CancellationToken, ValueTask<byte[]>> computeHashAsync,
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

        return await ComputeFileHashAsync(stream, useUpperCase, computeHashAsync, cancellationToken);
    }

    /// <summary>
    /// 说明ComputeFileHashAsync在当前类型中的职责
    /// </summary>
    /// <param name="stream">用于提供stream</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="computeHashAsync">用于提供computeHashAsync</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
    public static async Task<string> ComputeFileHashAsync(
        Stream stream,
        bool useUpperCase,
        Func<Stream, CancellationToken, ValueTask<byte[]>> computeHashAsync,
        CancellationToken cancellationToken)
    {
        Check.NotNull(stream);
        Check.NotNull(computeHashAsync);

        var hash = await computeHashAsync(stream, cancellationToken);
        return HexEncoding.ToHex(hash, useUpperCase);
    }

    /// <summary>
    /// 说明ComputeMd5FileHashAsync在当前类型中的职责
    /// </summary>
    /// <param name="filePath">用于提供filePath</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="computeHashAsync">用于提供computeHashAsync</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
    public static async Task<string> ComputeMd5FileHashAsync(
        string filePath,
        bool useUpperCase,
        bool useShortHash,
        Func<Stream, CancellationToken, ValueTask<byte[]>> computeHashAsync,
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

        return await ComputeMd5FileHashAsync(stream, useUpperCase, useShortHash, computeHashAsync, cancellationToken);
    }

    /// <summary>
    /// 说明ComputeMd5FileHashAsync在当前类型中的职责
    /// </summary>
    /// <param name="stream">用于提供stream</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="computeHashAsync">用于提供computeHashAsync</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
    public static async Task<string> ComputeMd5FileHashAsync(
        Stream stream,
        bool useUpperCase,
        bool useShortHash,
        Func<Stream, CancellationToken, ValueTask<byte[]>> computeHashAsync,
        CancellationToken cancellationToken)
    {
        Check.NotNull(stream);
        Check.NotNull(computeHashAsync);

        var hash = await computeHashAsync(stream, cancellationToken);
        return FormatMd5Hash(hash, useUpperCase, useShortHash);
    }

    /// <summary>
    /// 说明VerifyHash在当前类型中的职责
    /// </summary>
    /// <param name="input">用于提供nput</param>
    /// <param name="hash">用于提供hash</param>
    /// <param name="encoding">用于提供encoding</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static bool VerifyHash(
        string input,
        string hash,
        Encoding? encoding,
        Func<byte[], byte[]> computeHash)
    {
        Check.NotNull(hash);

        var computedHash = ComputeHash(input, useUpperCase: false, encoding, computeHash);
        return HashComparison.FixedTimeEqualsHex(computedHash, hash);
    }

    /// <summary>
    /// 说明VerifyHash在当前类型中的职责
    /// </summary>
    /// <param name="bytes">用于提供bytes</param>
    /// <param name="hash">用于提供hash</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static bool VerifyHash(byte[] bytes, string hash, Func<byte[], byte[]> computeHash)
    {
        Check.NotNull(hash);

        var computedHash = ComputeHash(bytes, useUpperCase: false, computeHash);
        return HashComparison.FixedTimeEqualsHex(computedHash, hash);
    }

    /// <summary>
    /// 说明VerifyMd5Hash在当前类型中的职责
    /// </summary>
    /// <param name="input">用于提供nput</param>
    /// <param name="hash">用于提供hash</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="encoding">用于提供encoding</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static bool VerifyMd5Hash(
        string input,
        string hash,
        bool useShortHash,
        Encoding? encoding,
        Func<byte[], byte[]> computeHash)
    {
        Check.NotNull(hash);

        var computedHash = ComputeMd5Hash(input, useUpperCase: false, useShortHash, encoding, computeHash);
        return HashComparison.FixedTimeEqualsHex(computedHash, hash);
    }

    /// <summary>
    /// 说明VerifyMd5Hash在当前类型中的职责
    /// </summary>
    /// <param name="bytes">用于提供bytes</param>
    /// <param name="hash">用于提供hash</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static bool VerifyMd5Hash(
        byte[] bytes,
        string hash,
        bool useShortHash,
        Func<byte[], byte[]> computeHash)
    {
        Check.NotNull(hash);

        var computedHash = ComputeMd5Hash(bytes, useUpperCase: false, useShortHash, computeHash);
        return HashComparison.FixedTimeEqualsHex(computedHash, hash);
    }

    /// <summary>
    /// 说明FormatMd5Hash在当前类型中的职责
    /// </summary>
    /// <param name="hash">用于提供hash</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static string FormatMd5Hash(byte[] hash, bool useUpperCase, bool useShortHash)
    {
        var hashString = HexEncoding.ToHex(hash, useUpperCase);
        return useShortHash ? hashString.Substring(8, 16) : hashString;
    }
}

/// <summary>
/// 封装Sha3Hash相关的数据和行为
/// </summary>
internal static class Sha3Hash
{
    /// <summary>
    /// 说明Hash256在当前类型中的职责
    /// </summary>
    /// <param name="bytes">用于提供bytes</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static byte[] Hash256(byte[] bytes)
    {
        return SHA3_256.IsSupported ? SHA3_256.HashData(bytes) : ComputeSha3(bytes, hashLength: 32, rateBytes: 136);
    }

    /// <summary>
    /// 说明Hash384在当前类型中的职责
    /// </summary>
    /// <param name="bytes">用于提供bytes</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static byte[] Hash384(byte[] bytes)
    {
        return SHA3_384.IsSupported ? SHA3_384.HashData(bytes) : ComputeSha3(bytes, hashLength: 48, rateBytes: 104);
    }

    /// <summary>
    /// 说明Hash512在当前类型中的职责
    /// </summary>
    /// <param name="bytes">用于提供bytes</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static byte[] Hash512(byte[] bytes)
    {
        return SHA3_512.IsSupported ? SHA3_512.HashData(bytes) : ComputeSha3(bytes, hashLength: 64, rateBytes: 72);
    }

    /// <summary>
    /// 说明Hash256Async在当前类型中的职责
    /// </summary>
    /// <param name="stream">用于提供stream</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的byte</returns>
    public static async ValueTask<byte[]> Hash256Async(Stream stream, CancellationToken cancellationToken)
    {
        if (SHA3_256.IsSupported)
        {
            return await SHA3_256.HashDataAsync(stream, cancellationToken);
        }

        return await ComputeSha3Async(stream, hashLength: 32, rateBytes: 136, cancellationToken);
    }

    /// <summary>
    /// 说明Hash384Async在当前类型中的职责
    /// </summary>
    /// <param name="stream">用于提供stream</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的byte</returns>
    public static async ValueTask<byte[]> Hash384Async(Stream stream, CancellationToken cancellationToken)
    {
        if (SHA3_384.IsSupported)
        {
            return await SHA3_384.HashDataAsync(stream, cancellationToken);
        }

        return await ComputeSha3Async(stream, hashLength: 48, rateBytes: 104, cancellationToken);
    }

    /// <summary>
    /// 说明Hash512Async在当前类型中的职责
    /// </summary>
    /// <param name="stream">用于提供stream</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的byte</returns>
    public static async ValueTask<byte[]> Hash512Async(Stream stream, CancellationToken cancellationToken)
    {
        if (SHA3_512.IsSupported)
        {
            return await SHA3_512.HashDataAsync(stream, cancellationToken);
        }

        return await ComputeSha3Async(stream, hashLength: 64, rateBytes: 72, cancellationToken);
    }

    /// <summary>
    /// 说明ComputeSha3Async在当前类型中的职责
    /// </summary>
    /// <param name="stream">用于提供stream</param>
    /// <param name="hashLength">用于提供hashLength</param>
    /// <param name="rateBytes">用于提供rateBytes</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的byte</returns>
    private static async ValueTask<byte[]> ComputeSha3Async(
        Stream stream,
        int hashLength,
        int rateBytes,
        CancellationToken cancellationToken)
    {
        Check.NotNull(stream);

        var state = new ulong[25];
        var pendingBlock = new byte[rateBytes];
        var pendingCount = 0;
        var buffer = new byte[81920];

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            var offset = 0;
            while (offset < bytesRead)
            {
                var bytesToCopy = Math.Min(rateBytes - pendingCount, bytesRead - offset);
                buffer.AsSpan(offset, bytesToCopy).CopyTo(pendingBlock.AsSpan(pendingCount));
                pendingCount += bytesToCopy;
                offset += bytesToCopy;

                if (pendingCount == rateBytes)
                {
                    AbsorbBlock(state, pendingBlock);
                    KeccakF1600(state);
                    pendingCount = 0;
                }
            }
        }

        return FinalizeSha3(state, pendingBlock.AsSpan(0, pendingCount), hashLength, rateBytes);
    }

    /// <summary>
    /// 说明ComputeSha3在当前类型中的职责
    /// </summary>
    /// <param name="bytes">用于提供bytes</param>
    /// <param name="hashLength">用于提供hashLength</param>
    /// <param name="rateBytes">用于提供rateBytes</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static byte[] ComputeSha3(byte[] bytes, int hashLength, int rateBytes)
    {
        Check.NotNull(bytes);

        var state = new ulong[25];
        var offset = 0;

        while (bytes.Length - offset >= rateBytes)
        {
            AbsorbBlock(state, bytes.AsSpan(offset, rateBytes));
            KeccakF1600(state);
            offset += rateBytes;
        }

        return FinalizeSha3(state, bytes.AsSpan(offset), hashLength, rateBytes);
    }

    /// <summary>
    /// 说明FinalizeSha3在当前类型中的职责
    /// </summary>
    /// <param name="state">用于提供状态</param>
    /// <param name="tail">用于提供tail</param>
    /// <param name="hashLength">用于提供hashLength</param>
    /// <param name="rateBytes">用于提供rateBytes</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static byte[] FinalizeSha3(ulong[] state, ReadOnlySpan<byte> tail, int hashLength, int rateBytes)
    {
        var finalBlock = new byte[rateBytes];
        tail.CopyTo(finalBlock);
        finalBlock[tail.Length] = 0x06;
        finalBlock[^1] |= 0x80;
        AbsorbBlock(state, finalBlock);
        KeccakF1600(state);

        var output = new byte[hashLength];
        var outputOffset = 0;

        while (outputOffset < output.Length)
        {
            for (var blockOffset = 0; blockOffset < rateBytes && outputOffset < output.Length; blockOffset++)
            {
                output[outputOffset++] = (byte)(state[blockOffset / 8] >> (8 * (blockOffset % 8)));
            }

            if (outputOffset < output.Length)
            {
                KeccakF1600(state);
            }
        }

        return output;
    }

    /// <summary>
    /// 说明AbsorbBlock在当前类型中的职责
    /// </summary>
    /// <param name="state">用于提供状态</param>
    /// <param name="block">用于提供block</param>
    private static void AbsorbBlock(ulong[] state, ReadOnlySpan<byte> block)
    {
        for (var index = 0; index < block.Length / sizeof(ulong); index++)
        {
            state[index] ^= BinaryPrimitives.ReadUInt64LittleEndian(block.Slice(index * sizeof(ulong), sizeof(ulong)));
        }
    }

    /// <summary>
    /// 说明KeccakF1600在当前类型中的职责
    /// </summary>
    /// <param name="state">用于提供状态</param>
    private static void KeccakF1600(ulong[] state)
    {
        Span<ulong> c = stackalloc ulong[5];
        Span<ulong> d = stackalloc ulong[5];
        Span<ulong> b = stackalloc ulong[25];

        for (var round = 0; round < RoundConstants.Length; round++)
        {
            for (var x = 0; x < 5; x++)
            {
                c[x] = state[x] ^ state[x + 5] ^ state[x + 10] ^ state[x + 15] ^ state[x + 20];
            }

            for (var x = 0; x < 5; x++)
            {
                d[x] = c[(x + 4) % 5] ^ RotateLeft(c[(x + 1) % 5], 1);
            }

            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 5; x++)
                {
                    state[x + (5 * y)] ^= d[x];
                }
            }

            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 5; x++)
                {
                    b[y + (5 * ((2 * x + 3 * y) % 5))] =
                        RotateLeft(state[x + (5 * y)], RotationOffsets[x + (5 * y)]);
                }
            }

            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 5; x++)
                {
                    state[x + (5 * y)] = b[x + (5 * y)] ^
                        ((~b[((x + 1) % 5) + (5 * y)]) & b[((x + 2) % 5) + (5 * y)]);
                }
            }

            state[0] ^= RoundConstants[round];
        }
    }

    /// <summary>
    /// 说明RotateLeft在当前类型中的职责
    /// </summary>
    /// <param name="value">用于转换、回显或断言的输入值</param>
    /// <param name="offset">用于提供offset</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static ulong RotateLeft(ulong value, int offset)
    {
        return offset == 0 ? value : (value << offset) | (value >> (64 - offset));
    }

    /// <summary>
    /// 保存当前类型处理流程依赖的RotationOffsets
    /// </summary>
    private static readonly int[] RotationOffsets =
    [
        0, 1, 62, 28, 27,
        36, 44, 6, 55, 20,
        3, 10, 43, 25, 39,
        41, 45, 15, 21, 8,
        18, 2, 61, 56, 14,
    ];

    /// <summary>
    /// 保存当前类型处理流程依赖的RoundConstants
    /// </summary>
    private static readonly ulong[] RoundConstants =
    [
        0x0000000000000001UL, 0x0000000000008082UL, 0x800000000000808aUL, 0x8000000080008000UL,
        0x000000000000808bUL, 0x0000000080000001UL, 0x8000000080008081UL, 0x8000000000008009UL,
        0x000000000000008aUL, 0x0000000000000088UL, 0x0000000080008009UL, 0x000000008000000aUL,
        0x000000008000808bUL, 0x800000000000008bUL, 0x8000000000008089UL, 0x8000000000008003UL,
        0x8000000000008002UL, 0x8000000000000080UL, 0x000000000000800aUL, 0x800000008000000aUL,
        0x8000000080008081UL, 0x8000000000008080UL, 0x0000000080000001UL, 0x8000000080008008UL,
    ];
}
