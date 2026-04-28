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

        if (System.IO.Path.IsPathRooted(fileName))
        {
            throw new ArgumentException("Rooted paths are not allowed.", nameof(fileName));
        }

        var rootPath = EnsureTrailingDirectorySeparator(System.IO.Path.GetFullPath(Path));
        var fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootPath, fileName));

        if (!fullPath.StartsWith(rootPath, PathComparison))
        {
            throw new ArgumentException("Path must stay within the temporary directory.", nameof(fileName));
        }

        return fullPath;
    }

    public FileInfo WriteAllText(string fileName, string contents, Encoding? encoding = null)
    {
        var filePath = GetPath(fileName);
        var directoryPath = System.IO.Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(filePath, contents, encoding ?? Encoding.UTF8);
        return new FileInfo(filePath);
    }

    public void Dispose()
    {
        if (!Directory.Exists(Path))
        {
            return;
        }

        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static string EnsureTrailingDirectorySeparator(string path)
    {
        if (path.EndsWith(System.IO.Path.DirectorySeparatorChar) ||
            path.EndsWith(System.IO.Path.AltDirectorySeparatorChar))
        {
            return path;
        }

        return path + System.IO.Path.DirectorySeparatorChar;
    }
}
