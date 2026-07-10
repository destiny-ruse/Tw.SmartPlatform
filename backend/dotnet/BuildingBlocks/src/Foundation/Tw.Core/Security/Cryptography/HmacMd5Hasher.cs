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

/// <summary>
/// 封装HMACComputation相关的数据和行为
/// </summary>
internal static class HmacComputation
{
    /// <summary>
    /// 保存当前类型处理流程依赖的默认Encoding
    /// </summary>
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// 说明ComputeHash在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="input">用于提供nput</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="encoding">用于提供encoding</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>方法计算得到的文本值</returns>
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

    /// <summary>
    /// 说明ComputeHash在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="bytes">用于提供bytes</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static string ComputeHash(byte[] key, byte[] bytes, bool useUpperCase, Func<byte[], byte[], byte[]> computeHash)
    {
        Check.NotNull(key);
        Check.NotNull(bytes);
        Check.NotNull(computeHash);

        return HexEncoding.ToHex(computeHash(key, bytes), useUpperCase);
    }

    /// <summary>
    /// 说明ComputeMd5Hash在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="input">用于提供nput</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="encoding">用于提供encoding</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>方法计算得到的文本值</returns>
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

    /// <summary>
    /// 说明ComputeMd5Hash在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="bytes">用于提供bytes</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>方法计算得到的文本值</returns>
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

    /// <summary>
    /// 说明ComputeFileHashAsync在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="filePath">用于提供filePath</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="encoding">用于提供encoding</param>
    /// <param name="computeHashAsync">用于提供computeHashAsync</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
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

    /// <summary>
    /// 说明ComputeFileHashAsync在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="filePath">用于提供filePath</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="computeHashAsync">用于提供computeHashAsync</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
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

    /// <summary>
    /// 说明ComputeFileHashAsync在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="stream">用于提供stream</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="encoding">用于提供encoding</param>
    /// <param name="computeHashAsync">用于提供computeHashAsync</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
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

    /// <summary>
    /// 说明ComputeFileHashAsync在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="stream">用于提供stream</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="computeHashAsync">用于提供computeHashAsync</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
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

    /// <summary>
    /// 说明ComputeMd5FileHashAsync在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="filePath">用于提供filePath</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="encoding">用于提供encoding</param>
    /// <param name="computeHashAsync">用于提供computeHashAsync</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
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

    /// <summary>
    /// 说明ComputeMd5FileHashAsync在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="filePath">用于提供filePath</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="computeHashAsync">用于提供computeHashAsync</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
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

    /// <summary>
    /// 说明ComputeMd5FileHashAsync在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="stream">用于提供stream</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="encoding">用于提供encoding</param>
    /// <param name="computeHashAsync">用于提供computeHashAsync</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
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

    /// <summary>
    /// 说明ComputeMd5FileHashAsync在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="stream">用于提供stream</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="computeHashAsync">用于提供computeHashAsync</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的string</returns>
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

    /// <summary>
    /// 说明VerifyHash在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="input">用于提供nput</param>
    /// <param name="hash">用于提供hash</param>
    /// <param name="encoding">用于提供encoding</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
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

    /// <summary>
    /// 说明VerifyHash在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="bytes">用于提供bytes</param>
    /// <param name="hash">用于提供hash</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static bool VerifyHash(byte[] key, byte[] bytes, string hash, Func<byte[], byte[], byte[]> computeHash)
    {
        Check.NotNull(hash);

        var computedHash = ComputeHash(key, bytes, useUpperCase: false, computeHash);
        return HashComparison.FixedTimeEqualsHex(computedHash, hash);
    }

    /// <summary>
    /// 说明VerifyMd5Hash在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="input">用于提供nput</param>
    /// <param name="hash">用于提供hash</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="encoding">用于提供encoding</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
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

    /// <summary>
    /// 说明VerifyMd5Hash在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="bytes">用于提供bytes</param>
    /// <param name="hash">用于提供hash</param>
    /// <param name="useShortHash">用于提供useShortHash</param>
    /// <param name="computeHash">用于提供computeHash</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
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
/// 封装HMACSha3Hash相关的数据和行为
/// </summary>
internal static class HmacSha3Hash
{
    /// <summary>
    /// 说明Hash256在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="bytes">用于提供bytes</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static byte[] Hash256(byte[] key, byte[] bytes)
    {
        return HMACSHA3_256.IsSupported ? HMACSHA3_256.HashData(key, bytes) : ComputeHmac(key, bytes, Sha3Hash.Hash256, rateBytes: 136);
    }

    /// <summary>
    /// 说明Hash384在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="bytes">用于提供bytes</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static byte[] Hash384(byte[] key, byte[] bytes)
    {
        return HMACSHA3_384.IsSupported ? HMACSHA3_384.HashData(key, bytes) : ComputeHmac(key, bytes, Sha3Hash.Hash384, rateBytes: 104);
    }

    /// <summary>
    /// 说明Hash512在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="bytes">用于提供bytes</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static byte[] Hash512(byte[] key, byte[] bytes)
    {
        return HMACSHA3_512.IsSupported ? HMACSHA3_512.HashData(key, bytes) : ComputeHmac(key, bytes, Sha3Hash.Hash512, rateBytes: 72);
    }

    /// <summary>
    /// 说明Hash256Async在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="stream">用于提供stream</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的byte</returns>
    public static async ValueTask<byte[]> Hash256Async(byte[] key, Stream stream, CancellationToken cancellationToken)
    {
        if (HMACSHA3_256.IsSupported)
        {
            return await HMACSHA3_256.HashDataAsync(key, stream, cancellationToken);
        }

        return await ComputeHmacAsync(key, stream, Sha3Hash.Hash256, Sha3Hash.Hash256Async, rateBytes: 136, cancellationToken);
    }

    /// <summary>
    /// 说明Hash384Async在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="stream">用于提供stream</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的byte</returns>
    public static async ValueTask<byte[]> Hash384Async(byte[] key, Stream stream, CancellationToken cancellationToken)
    {
        if (HMACSHA3_384.IsSupported)
        {
            return await HMACSHA3_384.HashDataAsync(key, stream, cancellationToken);
        }

        return await ComputeHmacAsync(key, stream, Sha3Hash.Hash384, Sha3Hash.Hash384Async, rateBytes: 104, cancellationToken);
    }

    /// <summary>
    /// 说明Hash512Async在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="stream">用于提供stream</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的byte</returns>
    public static async ValueTask<byte[]> Hash512Async(byte[] key, Stream stream, CancellationToken cancellationToken)
    {
        if (HMACSHA3_512.IsSupported)
        {
            return await HMACSHA3_512.HashDataAsync(key, stream, cancellationToken);
        }

        return await ComputeHmacAsync(key, stream, Sha3Hash.Hash512, Sha3Hash.Hash512Async, rateBytes: 72, cancellationToken);
    }

    /// <summary>
    /// 说明ComputeHmac在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="bytes">用于提供bytes</param>
    /// <param name="hash">用于提供hash</param>
    /// <param name="rateBytes">用于提供rateBytes</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
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

    /// <summary>
    /// 说明ComputeHmacAsync在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="stream">用于提供stream</param>
    /// <param name="hash">用于提供hash</param>
    /// <param name="hashAsync">用于提供hashAsync</param>
    /// <param name="rateBytes">用于提供rateBytes</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的byte</returns>
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

    /// <summary>
    /// 创建键Block测试对象
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="hash">用于提供hash</param>
    /// <param name="rateBytes">用于提供rateBytes</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static byte[] CreateKeyBlock(byte[] key, Func<byte[], byte[]> hash, int rateBytes)
    {
        var normalizedKey = key.Length > rateBytes ? hash(key) : key;
        var keyBlock = new byte[rateBytes];
        normalizedKey.AsSpan().CopyTo(keyBlock);

        return keyBlock;
    }

    /// <summary>
    /// 创建Pad测试对象
    /// </summary>
    /// <param name="keyBlock">用于提供键Block</param>
    /// <param name="value">用于转换、回显或断言的输入值</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static byte[] CreatePad(byte[] keyBlock, byte value)
    {
        var pad = new byte[keyBlock.Length];

        for (var index = 0; index < pad.Length; index++)
        {
            pad[index] = (byte)(keyBlock[index] ^ value);
        }

        return pad;
    }

    /// <summary>
    /// 说明Concat在当前类型中的职责
    /// </summary>
    /// <param name="prefix">用于提供前缀</param>
    /// <param name="suffix">用于提供suffix</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static byte[] Concat(byte[] prefix, byte[] suffix)
    {
        var bytes = new byte[prefix.Length + suffix.Length];
        prefix.AsSpan().CopyTo(bytes);
        suffix.AsSpan().CopyTo(bytes.AsSpan(prefix.Length));

        return bytes;
    }

    /// <summary>
    /// 封装前缀Stream相关的数据和行为
    /// </summary>
    private sealed class PrefixStream(byte[] prefix, Stream innerStream) : Stream
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的前缀Offset
        /// </summary>
        private int _prefixOffset;

        /// <summary>
        /// CanRead在当前对象中的业务含义
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// CanSeek在当前对象中的业务含义
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// CanWrite在当前对象中的业务含义
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// 不支持异常在当前对象中的业务含义
        /// </summary>
        public override long Length => throw new NotSupportedException("前缀流不支持获取长度。");

        /// <summary>
        /// 当前对象用于完成处理流程的内部状态
        /// </summary>
        public override long Position
        {
            get => throw new NotSupportedException("前缀流不支持获取位置。");
            set => throw new NotSupportedException("前缀流不支持设置位置。");
        }

        /// <summary>
        /// 说明Flush在当前类型中的职责
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// 读取当前数据源中的下一段内容
        /// </summary>
        /// <param name="buffer">用于提供buffer</param>
        /// <param name="offset">用于提供offset</param>
        /// <param name="count">用于构造测试输入或断言的数量</param>
        /// <returns>方法完成后返回给调用方的结果对象</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        /// <summary>
        /// 读取当前数据源中的下一段内容
        /// </summary>
        /// <param name="buffer">用于提供buffer</param>
        /// <returns>方法完成后返回给调用方的结果对象</returns>
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

        /// <summary>
        /// 读取异步内容
        /// </summary>
        /// <param name="buffer">用于提供buffer</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的int</returns>
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

        /// <summary>
        /// 说明Seek在当前类型中的职责
        /// </summary>
        /// <param name="offset">用于提供offset</param>
        /// <param name="origin">用于提供origin</param>
        /// <returns>方法完成后返回给调用方的结果对象</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("前缀流不支持定位。");
        }

        /// <summary>
        /// 说明写入Length在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("前缀流不支持设置长度。");
        }

        /// <summary>
        /// 说明写入在当前类型中的职责
        /// </summary>
        /// <param name="buffer">用于提供buffer</param>
        /// <param name="offset">用于提供offset</param>
        /// <param name="count">用于构造测试输入或断言的数量</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("前缀流不支持写入。");
        }

        /// <summary>
        /// 说明释放在当前类型中的职责
        /// </summary>
        /// <param name="disposing">用于提供disposing</param>
        protected override void Dispose(bool disposing)
        {
            _prefixOffset = prefix.Length;
            base.Dispose(disposing);
        }
    }
}
