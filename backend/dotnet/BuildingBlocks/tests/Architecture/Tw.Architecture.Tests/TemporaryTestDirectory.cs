namespace Tw.Architecture.Tests;

/// <summary>
/// 为文件驱动架构测试提供自动清理的隔离目录
/// </summary>
internal sealed class TemporaryTestDirectory : IDisposable
{
    /// <summary>
    /// 当前测试独占的临时目录
    /// </summary>
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        $"tw-architecture-{Guid.NewGuid():N}");

    /// <summary>
    /// 初始化临时目录并确保后续文件写入具备根路径
    /// </summary>
    internal TemporaryTestDirectory()
    {
        Directory.CreateDirectory(_rootPath);
    }

    /// <summary>
    /// 在隔离目录中创建测试文件及其父目录
    /// </summary>
    /// <param name="relativePath">相对隔离目录的文件路径</param>
    /// <param name="content">写入文件的完整文本</param>
    /// <returns>可供被测解析器读取的绝对路径</returns>
    internal string WriteFile(string relativePath, string content)
    {
        var path = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// 删除测试创建的全部临时文件与目录
    /// </summary>
    public void Dispose()
    {
        Directory.Delete(_rootPath, recursive: true);
    }
}
