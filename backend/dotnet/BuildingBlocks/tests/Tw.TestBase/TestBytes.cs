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

    public static byte[] AbcUtf8 => Encoding.UTF8.GetBytes(Text);

    public static byte[] LongTextUtf8 => Encoding.UTF8.GetBytes(LongText);

    public static byte[] HmacKeyUtf8 => Encoding.UTF8.GetBytes(HmacKey);
}
