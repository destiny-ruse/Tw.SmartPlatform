using System.Reflection;
using Tw.DependencyInjection.Diagnostics;

namespace Tw.DependencyInjection.Discovery;

/// <summary>发现结果：按拓扑排序的程序集与诊断报告</summary>
internal sealed record AssemblyDiscoveryResult(
    IReadOnlyList<Assembly> OrderedAssemblies, ServiceRegistrationReport Report);

internal static class AssemblyDiscoverer
{
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

        return new AssemblyDiscoveryResult(orderedAssemblies, report);
    }

    private static IReadOnlyList<string> ReferencedNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToList();
}
