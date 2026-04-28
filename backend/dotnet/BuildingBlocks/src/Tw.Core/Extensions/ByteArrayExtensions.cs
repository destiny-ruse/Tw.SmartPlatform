using System.Text;

namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for byte arrays.</summary>
public static class ByteArrayExtensions
{
    /// <summary>Converts a byte array to a hexadecimal string.</summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <param name="useUpperCase">Whether to use uppercase hexadecimal characters.</param>
    /// <returns>The hexadecimal representation of <paramref name="bytes"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is <see langword="null"/>.</exception>
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
