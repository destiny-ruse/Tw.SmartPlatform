using System.Reflection;

namespace Tw.Reflection;

/// <summary>
/// 从显式提供的程序集中查找运行时类型
/// </summary>
public sealed class TypeFinder : ITypeFinder
{
    /// <summary>表示 SkippedAssemblyNamePrefixes 字段</summary>
    private static readonly string[] SkippedAssemblyNamePrefixes = ["System", "Microsoft", "Windows"];

    /// <summary>
    /// 初始化 <see cref="TypeFinder"/> 类的新实例
    /// </summary>
    /// <param name="assemblies">要搜索的程序集，保留首次出现顺序</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="assemblies"/> 或其中任一项为 <see langword="null"/> 时抛出</exception>
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

    /// <summary>执行 ShouldSkipAssembly 操作</summary>
    /// <param name="assembly">assembly 参数</param>
    /// <returns>ShouldSkipAssembly 的执行结果</returns>
    private static bool ShouldSkipAssembly(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;

        return assemblyName is not null
            && SkippedAssemblyNamePrefixes.Any(prefix =>
                assemblyName.Equals(prefix, StringComparison.Ordinal)
                || assemblyName.StartsWith(prefix + ".", StringComparison.Ordinal));
    }

    /// <summary>执行 GetLoadableTypes 操作</summary>
    /// <param name="assembly">assembly 参数</param>
    /// <returns>GetLoadableTypes 的执行结果</returns>
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

    /// <summary>执行 IsConcrete 操作</summary>
    /// <param name="type">type 参数</param>
    /// <returns>IsConcrete 的执行结果</returns>
    private static bool IsConcrete(Type type)
    {
        return !type.IsAbstract && !type.IsInterface && !type.ContainsGenericParameters;
    }
}
