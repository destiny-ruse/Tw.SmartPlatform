using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Tw.DependencyInjection.Discovery;

/// <summary>
/// 合并 AppDomain 已加载程序集与依赖上下文的默认候选来源
/// </summary>
internal sealed class RuntimeAssemblySource : IAssemblySource
{
    /// <summary>
    /// 说明读取CandidateAssemblies在当前类型中的职责
    /// </summary>
    /// <returns>匹配当前查询条件的结果集合</returns>
    public IReadOnlyList<Assembly> GetCandidateAssemblies()
    {
        var byName = new Dictionary<string, Assembly>(StringComparer.Ordinal);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            TryAdd(byName, assembly);
        }

        var context = DependencyContext.Default;
        if (context is not null)
        {
            foreach (var assemblyName in context.GetDefaultAssemblyNames())
            {
                try
                {
                    TryAdd(byName, Assembly.Load(assemblyName));
                }
                catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException)
                {
                }
            }
        }

        return byName.Values.ToList();
    }

    /// <summary>
    /// 说明尝试Add在当前类型中的职责
    /// </summary>
    /// <param name="byName">用于提供byName</param>
    /// <param name="assembly">用于提供assembly</param>
    private static void TryAdd(Dictionary<string, Assembly> byName, Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (name is not null)
        {
            byName[name] = assembly;
        }
    }
}
