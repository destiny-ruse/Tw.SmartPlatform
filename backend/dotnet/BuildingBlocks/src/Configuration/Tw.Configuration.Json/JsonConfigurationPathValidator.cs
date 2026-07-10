namespace Tw.Configuration.Json;

/// <summary>
/// 封装JSONConfiguration路径Validator相关的数据和行为
/// </summary>
public sealed class JsonConfigurationPathValidator(string contentRoot, IReadOnlyList<string> allowedRoots)
{
    /// <summary>
    /// 保存当前类型处理流程依赖的contentRoot
    /// </summary>
    private readonly string _contentRoot = Normalize(contentRoot);
    /// <summary>
    /// 保存当前类型处理流程依赖的allowedRoots
    /// </summary>
    private readonly IReadOnlyList<string> _allowedRoots = allowedRoots.Select(Normalize).ToArray();

    /// <summary>
    /// 校验当前配置或输入约束，并在非法时抛出异常
    /// </summary>
    /// <param name="path">待处理文件或目录的路径</param>
    /// <returns>方法计算得到的文本值</returns>
    public string Validate(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (path.Contains('*', StringComparison.Ordinal))
        {
            throw new ConfigurationPathException("Configuration wildcard scanning is not allowed.");
        }

        var fullPath = Normalize(path);
        var roots = _allowedRoots.Count > 0 ? _allowedRoots : [_contentRoot];
        if (!roots.Any(root => IsUnderRoot(fullPath, root)))
        {
            throw new ConfigurationPathException($"Configuration path '{fullPath}' is outside allowed configuration roots.");
        }

        return fullPath;
    }

    /// <summary>
    /// 说明Normalize在当前类型中的职责
    /// </summary>
    /// <param name="path">待处理文件或目录的路径</param>
    /// <returns>方法计算得到的文本值</returns>
    private static string Normalize(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// 判断Under根目录是否满足条件
    /// </summary>
    /// <param name="path">待处理文件或目录的路径</param>
    /// <param name="root">用于提供根目录</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsUnderRoot(string path, string root)
    {
        return string.Equals(path, root, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
