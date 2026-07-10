using System.Reflection;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Registration;

namespace Tw.DependencyInjection.Discovery;

/// <summary>
/// 发现结果：按拓扑排序的程序集、诊断报告与程序集可达性图
/// </summary>
internal sealed record AssemblyDiscoveryResult(
    IReadOnlyList<Assembly> OrderedAssemblies,
    ServiceRegistrationReport Report,
    AssemblyReachabilityGraph ReachabilityGraph);

/// <summary>
/// 封装AssemblyDiscoverer相关的数据和行为
/// </summary>
internal static class AssemblyDiscoverer
{
    /// <summary>
    /// 说明Discover在当前类型中的职责
    /// </summary>
    /// <param name="options">用于配置当前组件行为的选项</param>
    /// <param name="source">用于提供source</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static AssemblyDiscoveryResult Discover(ServiceRegistrationOptions options, IAssemblySource source)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(source);

        var byName = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        foreach (var assembly in source.GetCandidateAssemblies())
        {
            var name = assembly.GetName().Name;
            if (name is not null)
            {
                byName[name] = assembly;
            }
        }

        var included = AssemblyFilter.Filter(byName.Keys, options);
        var includedSet = new HashSet<string>(included, StringComparer.Ordinal);

        var excluded = byName.Keys
            .Where(name => !includedSet.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var descriptors = included
            .Select(name => new AssemblyDescriptor(name, ReferencedNames(byName[name])))
            .ToList();

        var topology = AssemblyTopologySorter.Sort(descriptors);
        var orderedAssemblies = topology.Select(entry => byName[entry.AssemblyName]).ToList();

        var report = new ServiceRegistrationReport(
            topology.Select(entry => entry.AssemblyName).ToList(),
            excluded,
            topology);

        var referencesByAssemblyName = descriptors.ToDictionary(
            descriptor => descriptor.Name,
            descriptor => descriptor.ReferencedAssemblyNames
                // 只保留扫描范围内的引用，剔除框架与第三方程序集，避免可达图扩散到扫描边界之外
                .Where(includedSet.Contains)
                .ToList()
                as IReadOnlyList<string>,
            StringComparer.Ordinal);

        return new AssemblyDiscoveryResult(
            orderedAssemblies,
            report,
            new AssemblyReachabilityGraph(referencesByAssemblyName));
    }

    /// <summary>
    /// 说明Referenced名称集合在当前类型中的职责
    /// </summary>
    /// <param name="assembly">用于提供assembly</param>
    /// <returns>方法计算得到的文本值</returns>
    private static IReadOnlyList<string> ReferencedNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToList();
}
