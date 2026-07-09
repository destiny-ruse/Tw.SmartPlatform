namespace Tw.Configuration.Json;

/// <summary>表示 JsonConfigurationPathValidator 类型</summary>
public sealed class JsonConfigurationPathValidator(string contentRoot, IReadOnlyList<string> allowedRoots)
{
    /// <summary>表示 _contentRoot 字段</summary>
    private readonly string _contentRoot = Normalize(contentRoot);
    /// <summary>表示 _allowedRoots 字段</summary>
    private readonly IReadOnlyList<string> _allowedRoots = allowedRoots.Select(Normalize).ToArray();

    /// <summary>执行 Validate 操作</summary>
    /// <param name="path">path 参数</param>
    /// <returns>Validate 的执行结果</returns>
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

    /// <summary>执行 Normalize 操作</summary>
    /// <param name="path">path 参数</param>
    /// <returns>Normalize 的执行结果</returns>
    private static string Normalize(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>执行 IsUnderRoot 操作</summary>
    /// <param name="path">path 参数</param>
    /// <param name="root">root 参数</param>
    /// <returns>IsUnderRoot 的执行结果</returns>
    private static bool IsUnderRoot(string path, string root)
    {
        return string.Equals(path, root, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
