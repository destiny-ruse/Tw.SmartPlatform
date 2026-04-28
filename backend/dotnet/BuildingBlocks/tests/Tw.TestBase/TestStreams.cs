using System.Text;

namespace Tw.TestBase;

public static class TestStreams
{
    public static MemoryStream FromText(string value, Encoding? encoding = null)
    {
        return new MemoryStream((encoding ?? Encoding.UTF8).GetBytes(value));
    }

    public static MemoryStream FromBytes(byte[] bytes)
    {
        return new MemoryStream(bytes.ToArray());
    }
}
