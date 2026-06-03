using Tw.Core;

namespace Tw.Core.Security.Cryptography;

internal static class HexEncoding
{
    public static string ToHex(byte[] bytes, bool useUpperCase = false)
    {
        Check.NotNull(bytes);

        var hex = Convert.ToHexString(bytes);
        return useUpperCase ? hex : hex.ToLowerInvariant();
    }

    public static byte[] FromHex(string hex)
    {
        Check.NotNull(hex);

        return Convert.FromHexString(hex);
    }
}
