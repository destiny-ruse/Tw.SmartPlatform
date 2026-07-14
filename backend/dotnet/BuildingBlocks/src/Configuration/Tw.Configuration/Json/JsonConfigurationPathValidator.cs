namespace Tw.Configuration.Json;

/// <summary>
/// 将 JSON 配置文件限制在明确允许的根目录内
/// </summary>
public sealed class JsonConfigurationPathValidator
{
    /// <summary>
    /// 未配置显式允许根目录时采用的内容根目录
    /// </summary>
    private readonly string _contentRoot;

    /// <summary>
    /// 可以读取 JSON 配置文件的规范化根目录
    /// </summary>
    private readonly IReadOnlyList<string> _allowedRoots;

    /// <summary>
    /// 创建仅允许访问指定配置根目录的路径校验器
    /// </summary>
    /// <param name="contentRoot">未提供允许根目录时采用的内容根目录</param>
    /// <param name="allowedRoots">可以读取配置文件的根目录集合，空集合表示使用内容根目录</param>
    /// <exception cref="ArgumentException">contentRoot 为空白时抛出</exception>
    /// <exception cref="ArgumentNullException">allowedRoots 为 null 时抛出</exception>
    public JsonConfigurationPathValidator(string contentRoot, IReadOnlyList<string> allowedRoots)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRoot);
        ArgumentNullException.ThrowIfNull(allowedRoots);

        _contentRoot = Normalize(contentRoot);
        _allowedRoots = allowedRoots.Select(Normalize).ToArray();
    }

    /// <summary>
    /// 校验配置文件存在且规范化路径位于允许根目录内
    /// </summary>
    /// <param name="path">需要加载的 JSON 配置文件路径</param>
    /// <returns>消除相对路径片段后的绝对文件路径</returns>
    /// <exception cref="ArgumentException">path 为空白时抛出</exception>
    /// <exception cref="ConfigurationPathException">路径包含通配符、越过允许根目录或文件不存在时抛出</exception>
    public string Validate(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (path.Contains('*', StringComparison.Ordinal))
        {
            throw new ConfigurationPathException("配置路径不得包含通配符");
        }

        var fullPath = Normalize(path);
        var roots = _allowedRoots.Count > 0 ? _allowedRoots : [_contentRoot];
        if (!roots.Any(root => IsUnderRoot(fullPath, root)))
        {
            throw new ConfigurationPathException($"配置路径 '{fullPath}' 不在允许的配置根目录内");
        }

        if (!File.Exists(fullPath))
        {
            throw new ConfigurationPathException($"配置文件不存在: '{fullPath}'");
        }

        return fullPath;
    }

    /// <summary>
    /// 将路径转换为绝对路径并移除非根路径末尾的目录分隔符
    /// </summary>
    /// <param name="path">需要规范化的文件或目录路径</param>
    /// <returns>使用当前平台路径格式的绝对路径</returns>
    private static string Normalize(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var pathRoot = Path.GetPathRoot(fullPath);
        return string.Equals(fullPath, pathRoot, PathComparison)
            ? fullPath
            : fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// 判断指定路径是否等于根目录或位于其后代目录中
    /// </summary>
    /// <param name="path">已经规范化的绝对文件路径</param>
    /// <param name="root">已经规范化的允许根目录</param>
    /// <returns>路径属于允许根目录时返回 <see langword="true"/></returns>
    private static bool IsUnderRoot(string path, string root)
    {
        if (string.Equals(path, root, PathComparison))
        {
            return true;
        }

        var rootPrefix = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;
        return path.StartsWith(rootPrefix, PathComparison);
    }

    /// <summary>
    /// 当前平台文件系统使用的路径比较规则
    /// </summary>
    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;
}
