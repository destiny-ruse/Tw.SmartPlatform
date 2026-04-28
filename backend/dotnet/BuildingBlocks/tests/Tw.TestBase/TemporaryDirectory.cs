using System.Text;

namespace Tw.TestBase;

public sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tw-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string GetPath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return System.IO.Path.Combine(Path, fileName);
    }

    public FileInfo WriteAllText(string fileName, string contents, Encoding? encoding = null)
    {
        var filePath = GetPath(fileName);
        File.WriteAllText(filePath, contents, encoding ?? Encoding.UTF8);
        return new FileInfo(filePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
