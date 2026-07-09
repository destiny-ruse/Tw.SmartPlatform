using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Tw.Core;

namespace Tw.Core.Security.Cryptography;

/// <summary>表示 HashComparison 类型</summary>
internal static class HashComparison
{
    /// <summary>执行 FixedTimeEqualsHex 操作</summary>
    /// <param name="expectedHash">expectedHash 参数</param>
    /// <param name="actualHash">actualHash 参数</param>
    /// <returns>FixedTimeEqualsHex 的执行结果</returns>
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

/// <summary>表示 HashComputation 类型</summary>
internal static class HashComputation
{
    /// <summary>表示 DefaultEncoding 字段</summary>
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>执行 ComputeHash 操作</summary>
    /// <param name="input">input 参数</param>
    /// <param name="useUpperCase">useUpperCase 参数</param>
    /// <param name="encoding">encoding 参数</param>
    /// <param name="computeHash">computeHash 参数</param>
    /// <returns>ComputeHash 的执行结果</returns>
    public static string ComputeHash(
        string input,
        bool useUpperCase,
        Encoding? encoding,
        Func<byte[], byte[]> computeHash)
    {
        Check.NotNullOrWhiteSpace(input);

        return ComputeHash((encoding ?? DefaultEncoding).GetBytes(input), useUpperCase, computeHash);
    }

    /// <summary>执行 ComputeHash 操作</summary>
    /// <param name="bytes">bytes 参数</param>
    /// <param name="useUpperCase">useUpperCase 参数</param>
    /// <param name="computeHash">computeHash 参数</param>
    /// <returns>ComputeHash 的执行结果</returns>
    public static string ComputeHash(byte[] bytes, bool useUpperCase, Func<byte[], byte[]> computeHash)
    {
        Check.NotNull(bytes);
        Check.NotNull(computeHash);

        return HexEncoding.ToHex(computeHash(bytes), useUpperCase);
    }

    /// <summary>执行 ComputeMd5Hash 操作</summary>
    /// <param name="input">input 参数</param>
    /// <param name="useUpperCase">useUpperCase 参数</param>
    /// <param name="useShortHash">useShortHash 参数</param>
    /// <param name="encoding">encoding 参数</param>
    /// <param name="computeHash">computeHash 参数</param>
    /// <returns>ComputeMd5Hash 的执行结果</returns>
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

    /// <summary>执行 ComputeMd5Hash 操作</summary>
    /// <param name="bytes">bytes 参数</param>
    /// <param name="useUpperCase">useUpperCase 参数</param>
    /// <param name="useShortHash">useShortHash 参数</param>
    /// <param name="computeHash">computeHash 参数</param>
    /// <returns>ComputeMd5Hash 的执行结果</returns>
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

    /// <summary>执行 ComputeFileHashAsync 操作</summary>
    /// <param name="filePath">filePath 参数</param>
    /// <param name="useUpperCase">useUpperCase 参数</param>
    /// <param name="computeHashAsync">computeHashAsync 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ComputeFileHashAsync 的执行结果</returns>
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

    /// <summary>执行 ComputeFileHashAsync 操作</summary>
    /// <param name="stream">stream 参数</param>
    /// <param name="useUpperCase">useUpperCase 参数</param>
    /// <param name="computeHashAsync">computeHashAsync 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ComputeFileHashAsync 的执行结果</returns>
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

    /// <summary>执行 ComputeMd5FileHashAsync 操作</summary>
    /// <param name="filePath">filePath 参数</param>
    /// <param name="useUpperCase">useUpperCase 参数</param>
    /// <param name="useShortHash">useShortHash 参数</param>
    /// <param name="computeHashAsync">computeHashAsync 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ComputeMd5FileHashAsync 的执行结果</returns>
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

    /// <summary>执行 ComputeMd5FileHashAsync 操作</summary>
    /// <param name="stream">stream 参数</param>
    /// <param name="useUpperCase">useUpperCase 参数</param>
    /// <param name="useShortHash">useShortHash 参数</param>
    /// <param name="computeHashAsync">computeHashAsync 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ComputeMd5FileHashAsync 的执行结果</returns>
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

    /// <summary>执行 VerifyHash 操作</summary>
    /// <param name="input">input 参数</param>
    /// <param name="hash">hash 参数</param>
    /// <param name="encoding">encoding 参数</param>
    /// <param name="computeHash">computeHash 参数</param>
    /// <returns>VerifyHash 的执行结果</returns>
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

    /// <summary>执行 VerifyHash 操作</summary>
    /// <param name="bytes">bytes 参数</param>
    /// <param name="hash">hash 参数</param>
    /// <param name="computeHash">computeHash 参数</param>
    /// <returns>VerifyHash 的执行结果</returns>
    public static bool VerifyHash(byte[] bytes, string hash, Func<byte[], byte[]> computeHash)
    {
        Check.NotNull(hash);

        var computedHash = ComputeHash(bytes, useUpperCase: false, computeHash);
        return HashComparison.FixedTimeEqualsHex(computedHash, hash);
    }

    /// <summary>执行 VerifyMd5Hash 操作</summary>
    /// <param name="input">input 参数</param>
    /// <param name="hash">hash 参数</param>
    /// <param name="useShortHash">useShortHash 参数</param>
    /// <param name="encoding">encoding 参数</param>
    /// <param name="computeHash">computeHash 参数</param>
    /// <returns>VerifyMd5Hash 的执行结果</returns>
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

    /// <summary>执行 VerifyMd5Hash 操作</summary>
    /// <param name="bytes">bytes 参数</param>
    /// <param name="hash">hash 参数</param>
    /// <param name="useShortHash">useShortHash 参数</param>
    /// <param name="computeHash">computeHash 参数</param>
    /// <returns>VerifyMd5Hash 的执行结果</returns>
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

    /// <summary>执行 FormatMd5Hash 操作</summary>
    /// <param name="hash">hash 参数</param>
    /// <param name="useUpperCase">useUpperCase 参数</param>
    /// <param name="useShortHash">useShortHash 参数</param>
    /// <returns>FormatMd5Hash 的执行结果</returns>
    private static string FormatMd5Hash(byte[] hash, bool useUpperCase, bool useShortHash)
    {
        var hashString = HexEncoding.ToHex(hash, useUpperCase);
        return useShortHash ? hashString.Substring(8, 16) : hashString;
    }
}

/// <summary>表示 Sha3Hash 类型</summary>
internal static class Sha3Hash
{
    /// <summary>执行 Hash256 操作</summary>
    /// <param name="bytes">bytes 参数</param>
    /// <returns>Hash256 的执行结果</returns>
    public static byte[] Hash256(byte[] bytes)
    {
        return SHA3_256.IsSupported ? SHA3_256.HashData(bytes) : ComputeSha3(bytes, hashLength: 32, rateBytes: 136);
    }

    /// <summary>执行 Hash384 操作</summary>
    /// <param name="bytes">bytes 参数</param>
    /// <returns>Hash384 的执行结果</returns>
    public static byte[] Hash384(byte[] bytes)
    {
        return SHA3_384.IsSupported ? SHA3_384.HashData(bytes) : ComputeSha3(bytes, hashLength: 48, rateBytes: 104);
    }

    /// <summary>执行 Hash512 操作</summary>
    /// <param name="bytes">bytes 参数</param>
    /// <returns>Hash512 的执行结果</returns>
    public static byte[] Hash512(byte[] bytes)
    {
        return SHA3_512.IsSupported ? SHA3_512.HashData(bytes) : ComputeSha3(bytes, hashLength: 64, rateBytes: 72);
    }

    /// <summary>执行 Hash256Async 操作</summary>
    /// <param name="stream">stream 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>Hash256Async 的执行结果</returns>
    public static async ValueTask<byte[]> Hash256Async(Stream stream, CancellationToken cancellationToken)
    {
        if (SHA3_256.IsSupported)
        {
            return await SHA3_256.HashDataAsync(stream, cancellationToken);
        }

        return await ComputeSha3Async(stream, hashLength: 32, rateBytes: 136, cancellationToken);
    }

    /// <summary>执行 Hash384Async 操作</summary>
    /// <param name="stream">stream 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>Hash384Async 的执行结果</returns>
    public static async ValueTask<byte[]> Hash384Async(Stream stream, CancellationToken cancellationToken)
    {
        if (SHA3_384.IsSupported)
        {
            return await SHA3_384.HashDataAsync(stream, cancellationToken);
        }

        return await ComputeSha3Async(stream, hashLength: 48, rateBytes: 104, cancellationToken);
    }

    /// <summary>执行 Hash512Async 操作</summary>
    /// <param name="stream">stream 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>Hash512Async 的执行结果</returns>
    public static async ValueTask<byte[]> Hash512Async(Stream stream, CancellationToken cancellationToken)
    {
        if (SHA3_512.IsSupported)
        {
            return await SHA3_512.HashDataAsync(stream, cancellationToken);
        }

        return await ComputeSha3Async(stream, hashLength: 64, rateBytes: 72, cancellationToken);
    }

    /// <summary>执行 ComputeSha3Async 操作</summary>
    /// <param name="stream">stream 参数</param>
    /// <param name="hashLength">hashLength 参数</param>
    /// <param name="rateBytes">rateBytes 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ComputeSha3Async 的执行结果</returns>
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

    /// <summary>执行 ComputeSha3 操作</summary>
    /// <param name="bytes">bytes 参数</param>
    /// <param name="hashLength">hashLength 参数</param>
    /// <param name="rateBytes">rateBytes 参数</param>
    /// <returns>ComputeSha3 的执行结果</returns>
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

    /// <summary>执行 FinalizeSha3 操作</summary>
    /// <param name="state">state 参数</param>
    /// <param name="tail">tail 参数</param>
    /// <param name="hashLength">hashLength 参数</param>
    /// <param name="rateBytes">rateBytes 参数</param>
    /// <returns>FinalizeSha3 的执行结果</returns>
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

    /// <summary>执行 AbsorbBlock 操作</summary>
    /// <param name="state">state 参数</param>
    /// <param name="block">block 参数</param>
    private static void AbsorbBlock(ulong[] state, ReadOnlySpan<byte> block)
    {
        for (var index = 0; index < block.Length / sizeof(ulong); index++)
        {
            state[index] ^= BinaryPrimitives.ReadUInt64LittleEndian(block.Slice(index * sizeof(ulong), sizeof(ulong)));
        }
    }

    /// <summary>执行 KeccakF1600 操作</summary>
    /// <param name="state">state 参数</param>
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

    /// <summary>执行 RotateLeft 操作</summary>
    /// <param name="value">value 参数</param>
    /// <param name="offset">offset 参数</param>
    /// <returns>RotateLeft 的执行结果</returns>
    private static ulong RotateLeft(ulong value, int offset)
    {
        return offset == 0 ? value : (value << offset) | (value >> (64 - offset));
    }

    /// <summary>表示 RotationOffsets 字段</summary>
    private static readonly int[] RotationOffsets =
    [
        0, 1, 62, 28, 27,
        36, 44, 6, 55, 20,
        3, 10, 43, 25, 39,
        41, 45, 15, 21, 8,
        18, 2, 61, 56, 14,
    ];

    /// <summary>表示 RoundConstants 字段</summary>
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
