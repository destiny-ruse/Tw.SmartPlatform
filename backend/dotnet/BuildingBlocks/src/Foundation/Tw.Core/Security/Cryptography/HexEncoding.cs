using Tw.Core;

namespace Tw.Core.Security.Cryptography;

/// <summary>表示 HexEncoding 类型</summary>
internal static class HexEncoding
{
    /// <summary>执行 ToHex 操作</summary>
    /// <param name="bytes">bytes 参数</param>
    /// <param name="useUpperCase">useUpperCase 参数</param>
    /// <returns>ToHex 的执行结果</returns>
    public static string ToHex(byte[] bytes, bool useUpperCase = false)
    {
        Check.NotNull(bytes);

        var hex = Convert.ToHexString(bytes);
        return useUpperCase ? hex : hex.ToLowerInvariant();
    }

    /// <summary>执行 FromHex 操作</summary>
    /// <param name="hex">hex 参数</param>
    /// <returns>FromHex 的执行结果</returns>
    public static byte[] FromHex(string hex)
    {
        Check.NotNull(hex);

        return Convert.FromHexString(hex);
    }
}
