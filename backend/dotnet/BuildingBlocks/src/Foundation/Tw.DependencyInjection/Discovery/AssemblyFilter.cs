namespace Tw.DependencyInjection.Discovery;

/// <summary>表示 AssemblyFilter 类型</summary>
internal static class AssemblyFilter
{
    /// <summary>表示 DefaultPrefix 常量</summary>
    private const string DefaultPrefix = "Tw.";

    /// <summary>执行 Filter 操作</summary>
    /// <param name="assemblyNames">assemblyNames 参数</param>
    /// <param name="options">options 参数</param>
    /// <returns>Filter 的执行结果</returns>
    public static IReadOnlyList<string> Filter(
        IEnumerable<string> assemblyNames, ServiceRegistrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(assemblyNames);
        ArgumentNullException.ThrowIfNull(options);

        var included = new List<string>();
        foreach (var name in assemblyNames)
        {
            if (IsIncluded(name, options) && !IsExcluded(name, options))
            {
                included.Add(name);
            }
        }

        return included;
    }

    /// <summary>执行 IsIncluded 操作</summary>
    /// <param name="name">name 参数</param>
    /// <param name="options">options 参数</param>
    /// <returns>IsIncluded 的执行结果</returns>
    private static bool IsIncluded(string name, ServiceRegistrationOptions options)
    {
        if (options.IncludeAssemblies.Contains(name, StringComparer.Ordinal))
        {
            return true;
        }

        if (name.StartsWith(DefaultPrefix, StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var prefix in options.IncludeAssemblyPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>执行 IsExcluded 操作</summary>
    /// <param name="name">name 参数</param>
    /// <param name="options">options 参数</param>
    /// <returns>IsExcluded 的执行结果</returns>
    private static bool IsExcluded(string name, ServiceRegistrationOptions options)
    {
        if (options.ExcludeAssemblies.Contains(name, StringComparer.Ordinal))
        {
            return true;
        }

        foreach (var prefix in options.ExcludeAssemblyPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
