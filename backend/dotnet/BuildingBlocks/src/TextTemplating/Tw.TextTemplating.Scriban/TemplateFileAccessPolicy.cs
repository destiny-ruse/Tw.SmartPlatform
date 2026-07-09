namespace Tw.TextTemplating.Scriban;

/// <summary>
/// 模板文件访问越界异常
/// </summary>
public sealed class TemplateFileAccessException : Exception
{
    /// <summary>
    /// 创建模板文件访问越界异常
    /// </summary>
    public TemplateFileAccessException()
        : base("模板文件只能从注册的模板根目录读取")
    {
    }
}

/// <summary>
/// 校验模板文件访问是否位于注册根目录内
/// </summary>
public sealed class TemplateFileAccessPolicy
{
    private readonly string[] _allowedRoots;

    /// <summary>
    /// 创建模板文件访问策略
    /// </summary>
    /// <param name="allowedRoots">允许访问的模板根目录集合</param>
    /// <exception cref="ArgumentNullException">allowedRoots 为 null 时抛出</exception>
    public TemplateFileAccessPolicy(IEnumerable<string> allowedRoots)
    {
        ArgumentNullException.ThrowIfNull(allowedRoots);
        _allowedRoots = allowedRoots.Select(NormalizeRoot).ToArray();
    }

    /// <summary>
    /// 校验并返回规范化后的模板文件完整路径
    /// </summary>
    /// <param name="path">模板文件路径</param>
    /// <returns>规范化后的模板文件完整路径</returns>
    /// <exception cref="ArgumentException">path 为空白时抛出</exception>
    /// <exception cref="TemplateFileAccessException">path 不在注册根目录内时抛出</exception>
    public string Validate(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        if (_allowedRoots.Any(root => fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
        {
            return fullPath;
        }

        throw new TemplateFileAccessException();
    }

    private static string NormalizeRoot(string root)
    {
        var fullPath = Path.GetFullPath(root);
        if (fullPath.EndsWith(Path.DirectorySeparatorChar))
        {
            return fullPath;
        }

        return fullPath + Path.DirectorySeparatorChar;
    }
}
