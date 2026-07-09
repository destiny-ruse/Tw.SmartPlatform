namespace Tw.Configuration.Json;

public sealed class JsonConfigurationPathValidator(string contentRoot, IReadOnlyList<string> allowedRoots)
{
    private readonly string _contentRoot = Normalize(contentRoot);
    private readonly IReadOnlyList<string> _allowedRoots = allowedRoots.Select(Normalize).ToArray();

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

    private static string Normalize(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool IsUnderRoot(string path, string root)
    {
        return string.Equals(path, root, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
