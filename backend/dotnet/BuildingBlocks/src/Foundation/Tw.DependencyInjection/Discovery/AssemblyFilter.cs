namespace Tw.DependencyInjection.Discovery;

internal static class AssemblyFilter
{
    private const string DefaultPrefix = "Tw.";

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
