using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Tw.DependencyInjection.Discovery;

/// <summary>合并 AppDomain 已加载程序集与依赖上下文的默认候选来源</summary>
internal sealed class RuntimeAssemblySource : IAssemblySource
{
    /// <summary>执行 GetCandidateAssemblies 操作</summary>
    /// <returns>GetCandidateAssemblies 的执行结果</returns>
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

    /// <summary>执行 TryAdd 操作</summary>
    /// <param name="byName">byName 参数</param>
    /// <param name="assembly">assembly 参数</param>
    private static void TryAdd(Dictionary<string, Assembly> byName, Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (name is not null)
        {
            byName[name] = assembly;
        }
    }
}
