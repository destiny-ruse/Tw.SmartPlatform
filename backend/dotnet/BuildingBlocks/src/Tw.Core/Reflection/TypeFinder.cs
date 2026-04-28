using System.Reflection;

namespace Tw.Core.Reflection;

/// <summary>
/// Finds runtime types from explicitly supplied assemblies.
/// </summary>
public sealed class TypeFinder : ITypeFinder
{
    private static readonly string[] SkippedAssemblyNamePrefixes = ["System", "Microsoft", "Windows"];

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeFinder"/> class.
    /// </summary>
    /// <param name="assemblies">The assemblies to search, preserving first occurrence order.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemblies"/> or one of its entries is <see langword="null"/>.</exception>
    public TypeFinder(IEnumerable<Assembly> assemblies)
    {
        var checkedAssemblies = Check.NotNull(assemblies);
        var distinctAssemblies = new List<Assembly>();
        var seenAssemblies = new HashSet<Assembly>();

        foreach (var assembly in checkedAssemblies)
        {
            var checkedAssembly = Check.NotNull(assembly, nameof(assemblies));
            if (seenAssemblies.Add(checkedAssembly))
            {
                distinctAssemblies.Add(checkedAssembly);
            }
        }

        Assemblies = distinctAssemblies;
    }

    /// <inheritdoc />
    public IReadOnlyList<Assembly> Assemblies { get; }

    /// <inheritdoc />
    public IReadOnlyList<Type> FindTypes()
    {
        var types = new List<Type>();

        foreach (var assembly in Assemblies)
        {
            if (ShouldSkipAssembly(assembly))
            {
                continue;
            }

            types.AddRange(GetLoadableTypes(assembly));
        }

        return types;
    }

    /// <inheritdoc />
    public IReadOnlyList<Type> FindTypes(Type baseType)
    {
        var checkedBaseType = Check.NotNull(baseType);

        return FindTypes()
            .Where(type => IsConcrete(type) && checkedBaseType.IsAssignableFrom(type))
            .ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<Type> FindTypes<TBaseType>()
    {
        return FindTypes(typeof(TBaseType));
    }

    private static bool ShouldSkipAssembly(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;

        return assemblyName is not null
            && SkippedAssemblyNamePrefixes.Any(prefix => assemblyName.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null)!;
        }
    }

    private static bool IsConcrete(Type type)
    {
        return !type.IsAbstract && !type.IsInterface;
    }
}
