using System.Security.Cryptography;
using System.Text;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// 提供 SHA-256 哈希计算与验证辅助方法
/// </summary>
public static class Sha256Hasher
{
    /// <summary>计算字符串的 SHA-256 哈希</summary>
    /// <param name="input">要计算哈希的字符串</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>十六进制字符串形式的 SHA-256 哈希</returns>
    public static string ComputeHash(string input, bool useUpperCase = false, Encoding? encoding = null)
    {
        return HashComputation.ComputeHash(input, useUpperCase, encoding, SHA256.HashData);
    }

    /// <summary>计算字节的 SHA-256 哈希</summary>
    /// <param name="bytes">要计算哈希的字节</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <returns>十六进制字符串形式的 SHA-256 哈希</returns>
    public static string ComputeHash(byte[] bytes, bool useUpperCase = false)
    {
        return HashComputation.ComputeHash(bytes, useUpperCase, SHA256.HashData);
    }

    /// <summary>计算文件的 SHA-256 哈希</summary>
    /// <param name="filePath">要读取的文件路径</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="cancellationToken">取消文件读取和哈希操作的令牌</param>
    /// <returns>十六进制字符串形式的 SHA-256 哈希</returns>
    public static Task<string> ComputeFileHashAsync(string filePath, bool useUpperCase = false, CancellationToken cancellationToken = default)
    {
        return HashComputation.ComputeFileHashAsync(filePath, useUpperCase, SHA256.HashDataAsync, cancellationToken);
    }

    /// <summary>计算流的 SHA-256 哈希且不释放该流</summary>
    /// <param name="stream">要从当前位置开始计算哈希的流</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="cancellationToken">取消流读取和哈希操作的令牌</param>
    /// <returns>十六进制字符串形式的 SHA-256 哈希</returns>
    public static Task<string> ComputeFileHashAsync(Stream stream, bool useUpperCase = false, CancellationToken cancellationToken = default)
    {
        return HashComputation.ComputeFileHashAsync(stream, useUpperCase, SHA256.HashDataAsync, cancellationToken);
    }

    /// <summary>使用固定时间字节比较验证字符串的 SHA-256 哈希</summary>
    /// <param name="input">要计算并验证哈希的字符串</param>
    /// <param name="hash">预期的十六进制哈希</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>哈希匹配时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool VerifyHash(string input, string hash, Encoding? encoding = null)
    {
        return HashComputation.VerifyHash(input, hash, encoding, SHA256.HashData);
    }

    /// <summary>使用固定时间字节比较验证字节的 SHA-256 哈希</summary>
    /// <param name="bytes">要计算并验证哈希的字节</param>
    /// <param name="hash">预期的十六进制哈希</param>
    /// <returns>哈希匹配时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool VerifyHash(byte[] bytes, string hash)
    {
        return HashComputation.VerifyHash(bytes, hash, SHA256.HashData);
    }
}
