using System.Text;

namespace Tw.TestBase;

public static class TestStreams
{
    public static MemoryStream FromText(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }

    public static MemoryStream FromBytes(byte[] bytes)
    {
        return new MemoryStream(bytes.ToArray());
    }
}
