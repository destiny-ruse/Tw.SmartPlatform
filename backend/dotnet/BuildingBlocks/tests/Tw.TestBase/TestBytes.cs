using System.Text;

namespace Tw.TestBase;

public static class TestBytes
{
    public const string Text = "abc";
    public const string LongText = "The quick brown fox jumps over the lazy dog";
    public const string HmacKey = "key";
    public const string AesKey16 = "0123456789abcdef";
    public const string DesKey8 = "12345678";
    public const string TripleDesKey24 = "0123456789abcdef01234567";

    public static byte[] AbcUtf8 => Utf8(Text);

    public static byte[] LongTextUtf8 => Utf8(LongText);

    public static byte[] HmacKeyUtf8 => Utf8(HmacKey);

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
