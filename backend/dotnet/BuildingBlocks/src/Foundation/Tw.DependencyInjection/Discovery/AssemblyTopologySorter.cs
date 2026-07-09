using Tw.DependencyInjection.Diagnostics;

namespace Tw.DependencyInjection.Discovery;

internal static class AssemblyTopologySorter
{
    public static IReadOnlyList<AssemblyTopologyEntry> Sort(IReadOnlyList<AssemblyDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var byName = descriptors.ToDictionary(d => d.Name, StringComparer.Ordinal);
        var state = new Dictionary<string, Mark>(StringComparer.Ordinal);
        var levels = new Dictionary<string, int>(StringComparer.Ordinal);
        var order = new List<string>();
        var path = new List<string>();

        foreach (var descriptor in descriptors)
        {
            Visit(descriptor.Name);
        }

        return order.Select(name => new AssemblyTopologyEntry(name, levels[name])).ToList();

        int Visit(string name)
        {
            if (state.TryGetValue(name, out var mark))
            {
                if (mark == Mark.InProgress)
                {
                    var cycleStart = path.IndexOf(name);
                    var chain = path.Skip(cycleStart).Append(name);
                    throw new ServiceRegistrationException(
                        "检测到程序集循环依赖: " + string.Join(" -> ", chain));
                }

                return levels[name];
            }

            state[name] = Mark.InProgress;
            path.Add(name);

            var level = 0;
            foreach (var reference in byName[name].ReferencedAssemblyNames)
            {
                if (byName.ContainsKey(reference))
                {
                    level = Math.Max(level, Visit(reference) + 1);
                }
            }

            path.RemoveAt(path.Count - 1);
            state[name] = Mark.Done;
            levels[name] = level;
            order.Add(name);
            return level;
        }
    }

    private enum Mark
    {
        InProgress,
        Done,
    }
}
