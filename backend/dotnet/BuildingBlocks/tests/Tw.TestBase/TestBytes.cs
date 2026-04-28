using System.Text;

namespace Tw.TestBase;

public static class TestBytes
{
    public static byte[] DeterministicBytes(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        var bytes = new byte[length];

        for (var index = 0; index < bytes.Length; index++)
        {
            bytes[index] = (byte)(index % 251);
        }

        return bytes;
    }

    public static byte[] Utf8(string text)
    {
        return Encoding.UTF8.GetBytes(text);
    }
}
