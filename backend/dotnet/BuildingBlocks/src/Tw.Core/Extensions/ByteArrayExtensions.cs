using System.Text;

namespace Tw.Core.Extensions;

/// <summary>提供字节数组扩展方法</summary>
public static class ByteArrayExtensions
{
    /// <summary>将字节数组转换为十六进制字符串</summary>
    /// <param name="bytes">要转换的字节</param>
    /// <param name="useUpperCase">是否使用大写十六进制字符</param>
    /// <returns><paramref name="bytes"/> 的十六进制表示</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="bytes"/> 为 <see langword="null"/> 时抛出</exception>
    public static string ToHexString(this byte[] bytes, bool useUpperCase = false)
    {
        Check.NotNull(bytes);

        var format = useUpperCase ? "X2" : "x2";
        var builder = new StringBuilder(bytes.Length * 2);

        foreach (var value in bytes)
        {
            builder.Append(value.ToString(format));
        }

        return builder.ToString();
    }
}
