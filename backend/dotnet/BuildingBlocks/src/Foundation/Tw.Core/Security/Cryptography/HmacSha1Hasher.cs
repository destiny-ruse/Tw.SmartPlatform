using System.Security.Cryptography;
using System.Text;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// 提供 HMAC-SHA1 哈希计算与验证辅助方法
/// </summary>
public static class HmacSha1Hasher
{
    /// <summary>计算字符串的 HMAC-SHA1 哈希</summary>
    /// <param name="key">HMAC 密钥</param>
    /// <param name="input">要计算哈希的字符串</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>十六进制字符串形式的 HMAC-SHA1 哈希</returns>
    public static string ComputeHash(string key, string input, bool useUpperCase = false, Encoding? encoding = null)
    {
        return HmacComputation.ComputeHash(key, input, useUpperCase, encoding, HMACSHA1.HashData);
    }

    /// <summary>计算字节的 HMAC-SHA1 哈希</summary>
    /// <param name="key">HMAC 密钥字节</param>
    /// <param name="bytes">要计算哈希的字节</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <returns>十六进制字符串形式的 HMAC-SHA1 哈希</returns>
    public static string ComputeHash(byte[] key, byte[] bytes, bool useUpperCase = false)
    {
        return HmacComputation.ComputeHash(key, bytes, useUpperCase, HMACSHA1.HashData);
    }

    /// <summary>计算文件的 HMAC-SHA1 哈希</summary>
    /// <param name="key">HMAC 密钥</param>
    /// <param name="filePath">要读取的文件路径</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <param name="cancellationToken">取消文件读取和哈希操作的令牌</param>
    /// <returns>十六进制字符串形式的 HMAC-SHA1 哈希</returns>
    public static Task<string> ComputeFileHashAsync(
        string key,
        string filePath,
        bool useUpperCase = false,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        return HmacComputation.ComputeFileHashAsync(key, filePath, useUpperCase, encoding, HMACSHA1.HashDataAsync, cancellationToken);
    }

    /// <summary>计算文件的 HMAC-SHA1 哈希</summary>
    /// <param name="key">HMAC 密钥字节</param>
    /// <param name="filePath">要读取的文件路径</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="cancellationToken">取消文件读取和哈希操作的令牌</param>
    /// <returns>十六进制字符串形式的 HMAC-SHA1 哈希</returns>
    public static Task<string> ComputeFileHashAsync(
        byte[] key,
        string filePath,
        bool useUpperCase = false,
        CancellationToken cancellationToken = default)
    {
        return HmacComputation.ComputeFileHashAsync(key, filePath, useUpperCase, HMACSHA1.HashDataAsync, cancellationToken);
    }

    /// <summary>计算流的 HMAC-SHA1 哈希且不释放该流</summary>
    /// <param name="key">HMAC 密钥</param>
    /// <param name="stream">要从当前位置开始计算哈希的流</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <param name="cancellationToken">取消流读取和哈希操作的令牌</param>
    /// <returns>十六进制字符串形式的 HMAC-SHA1 哈希</returns>
    public static Task<string> ComputeFileHashAsync(
        string key,
        Stream stream,
        bool useUpperCase = false,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        return HmacComputation.ComputeFileHashAsync(key, stream, useUpperCase, encoding, HMACSHA1.HashDataAsync, cancellationToken);
    }

    /// <summary>计算流的 HMAC-SHA1 哈希且不释放该流</summary>
    /// <param name="key">HMAC 密钥字节</param>
    /// <param name="stream">要从当前位置开始计算哈希的流</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="cancellationToken">取消流读取和哈希操作的令牌</param>
    /// <returns>十六进制字符串形式的 HMAC-SHA1 哈希</returns>
    public static Task<string> ComputeFileHashAsync(
        byte[] key,
        Stream stream,
        bool useUpperCase = false,
        CancellationToken cancellationToken = default)
    {
        return HmacComputation.ComputeFileHashAsync(key, stream, useUpperCase, HMACSHA1.HashDataAsync, cancellationToken);
    }

    /// <summary>使用固定时间字节比较验证字符串的 HMAC-SHA1 哈希</summary>
    /// <param name="key">HMAC 密钥</param>
    /// <param name="input">要计算并验证哈希的字符串</param>
    /// <param name="hash">预期的十六进制哈希</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>哈希匹配时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool VerifyHash(string key, string input, string hash, Encoding? encoding = null)
    {
        return HmacComputation.VerifyHash(key, input, hash, encoding, HMACSHA1.HashData);
    }

    /// <summary>使用固定时间字节比较验证字节的 HMAC-SHA1 哈希</summary>
    /// <param name="key">HMAC 密钥字节</param>
    /// <param name="bytes">要计算并验证哈希的字节</param>
    /// <param name="hash">预期的十六进制哈希</param>
    /// <returns>哈希匹配时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool VerifyHash(byte[] key, byte[] bytes, string hash)
    {
        return HmacComputation.VerifyHash(key, bytes, hash, HMACSHA1.HashData);
    }
}
