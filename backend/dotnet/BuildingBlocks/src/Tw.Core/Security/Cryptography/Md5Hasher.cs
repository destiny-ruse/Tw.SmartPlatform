using System.Security.Cryptography;
using System.Text;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// 提供 MD5 哈希计算与验证辅助方法
/// </summary>
public static class Md5Hasher
{
    /// <summary>
    /// 计算字符串的 MD5 哈希
    /// </summary>
    /// <param name="input">要计算哈希的字符串</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="useShortHash">是否返回旧版 MD5 哈希的中间 16 个字符片段</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>十六进制字符串形式的 MD5 哈希</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="input"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="input"/> 为空字符串或空白字符串时抛出</exception>
    public static string ComputeHash(string input, bool useUpperCase = false, bool useShortHash = false, Encoding? encoding = null)
    {
        return HashComputation.ComputeMd5Hash(input, useUpperCase, useShortHash, encoding, MD5.HashData);
    }

    /// <summary>
    /// 计算字节的 MD5 哈希
    /// </summary>
    /// <param name="bytes">要计算哈希的字节</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="useShortHash">是否返回旧版 MD5 哈希的中间 16 个字符片段</param>
    /// <returns>十六进制字符串形式的 MD5 哈希</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="bytes"/> 为 <see langword="null"/> 时抛出</exception>
    public static string ComputeHash(byte[] bytes, bool useUpperCase = false, bool useShortHash = false)
    {
        return HashComputation.ComputeMd5Hash(bytes, useUpperCase, useShortHash, MD5.HashData);
    }

    /// <summary>
    /// 计算文件的 MD5 哈希
    /// </summary>
    /// <param name="filePath">要读取的文件路径</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="useShortHash">是否返回旧版 MD5 哈希的中间 16 个字符片段</param>
    /// <param name="cancellationToken">取消文件读取和哈希操作的令牌</param>
    /// <returns>十六进制字符串形式的 MD5 哈希</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="filePath"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="filePath"/> 为空字符串或空白字符串时抛出</exception>
    public static Task<string> ComputeFileHashAsync(
        string filePath,
        bool useUpperCase = false,
        bool useShortHash = false,
        CancellationToken cancellationToken = default)
    {
        return HashComputation.ComputeMd5FileHashAsync(filePath, useUpperCase, useShortHash, MD5.HashDataAsync, cancellationToken);
    }

    /// <summary>
    /// 计算流的 MD5 哈希且不释放该流
    /// </summary>
    /// <param name="stream">要从当前位置开始计算哈希的流</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="useShortHash">是否返回旧版 MD5 哈希的中间 16 个字符片段</param>
    /// <param name="cancellationToken">取消流读取和哈希操作的令牌</param>
    /// <returns>十六进制字符串形式的 MD5 哈希</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="stream"/> 为 <see langword="null"/> 时抛出</exception>
    public static Task<string> ComputeFileHashAsync(
        Stream stream,
        bool useUpperCase = false,
        bool useShortHash = false,
        CancellationToken cancellationToken = default)
    {
        return HashComputation.ComputeMd5FileHashAsync(stream, useUpperCase, useShortHash, MD5.HashDataAsync, cancellationToken);
    }

    /// <summary>
    /// 使用固定时间字节比较验证字符串的 MD5 哈希
    /// </summary>
    /// <param name="input">要计算并验证哈希的字符串</param>
    /// <param name="hash">预期的十六进制哈希</param>
    /// <param name="useShortHash">是否按旧版 MD5 哈希的中间 16 个字符片段进行验证</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>哈希匹配时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="input"/> 或 <paramref name="hash"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="input"/> 为空字符串或空白字符串时抛出</exception>
    public static bool VerifyHash(string input, string hash, bool useShortHash = false, Encoding? encoding = null)
    {
        return HashComputation.VerifyMd5Hash(input, hash, useShortHash, encoding, MD5.HashData);
    }

    /// <summary>
    /// 使用固定时间字节比较验证字节的 MD5 哈希
    /// </summary>
    /// <param name="bytes">要计算并验证哈希的字节</param>
    /// <param name="hash">预期的十六进制哈希</param>
    /// <param name="useShortHash">是否按旧版 MD5 哈希的中间 16 个字符片段进行验证</param>
    /// <returns>哈希匹配时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="bytes"/> 或 <paramref name="hash"/> 为 <see langword="null"/> 时抛出</exception>
    public static bool VerifyHash(byte[] bytes, string hash, bool useShortHash = false)
    {
        return HashComputation.VerifyMd5Hash(bytes, hash, useShortHash, MD5.HashData);
    }
}
