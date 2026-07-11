using Tw.Core;

namespace Tw.Security.Cryptography;

/// <summary>
/// 封装HexEncoding相关的数据和行为
/// </summary>
internal static class HexEncoding
{
    /// <summary>
    /// 说明ToHex在当前类型中的职责
    /// </summary>
    /// <param name="bytes">用于提供bytes</param>
    /// <param name="useUpperCase">用于提供useUpperCase</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static string ToHex(byte[] bytes, bool useUpperCase = false)
    {
        Check.NotNull(bytes);

        var hex = Convert.ToHexString(bytes);
        return useUpperCase ? hex : hex.ToLowerInvariant();
    }

    /// <summary>
    /// 说明FromHex在当前类型中的职责
    /// </summary>
    /// <param name="hex">用于提供hex</param>
    /// <returns>方法计算得到的文本值</returns>
    public static byte[] FromHex(string hex)
    {
        Check.NotNull(hex);

        return Convert.FromHexString(hex);
    }
}
