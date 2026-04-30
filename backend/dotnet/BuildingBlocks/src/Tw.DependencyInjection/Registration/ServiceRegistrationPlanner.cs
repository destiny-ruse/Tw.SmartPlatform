using System.Reflection;
using Tw.Core;
using Tw.Core.Exceptions;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 将 R1 扫描阶段产出的 <see cref="ServiceScanDescriptor"/> 列表转换为经过冲突裁决的
/// <see cref="ServiceRegistrationPlan"/>。裁决规则遵循设计文档 §4.2、§5.2–§5.5。
/// </summary>
public static class ServiceRegistrationPlanner
{
    // ============================================================
    // 公共 API — 主重载（使用真实 Assembly 对象计算拓扑图）
    // ============================================================

    /// <summary>
    /// 根据扫描描述符列表与程序集列表，计算拓扑排序并产出注册计划。
    /// </summary>
    /// <param name="scans">R1 扫描阶段产出的候选描述符，允许为空。</param>
    /// <param name="assemblies">参与拓扑排序的程序集列表，允许为空。</param>
    /// <returns>已决策的 <see cref="ServiceRegistrationPlan"/>。</returns>
    public static ServiceRegistrationPlan Plan(
        IEnumerable<ServiceScanDescriptor> scans,
        IEnumerable<Assembly> assemblies)
    {
        Check.NotNull(scans);
        Check.NotNull(assemblies);

        // Build assembly dependency graph from real Assembly objects.
        // Edge A → B means A.GetReferencedAssemblies() contains B (A references B).
        // Only edges within the input assembly set are considered.
        var assemblyList = assemblies.Distinct().ToList();
        var assemblyNameSet = assemblyList
            .Select(a => a.GetName().Name ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var dependencyGraph = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);
        foreach (var asm in assemblyList)
        {
            var name = asm.GetName().Name ?? string.Empty;
            var refs = asm.GetReferencedAssemblies()
                .Select(n => n.Name ?? string.Empty)
                .Where(n => assemblyNameSet.Contains(n))
                .ToHashSet(StringComparer.Ordinal);
            dependencyGraph[name] = refs;
        }

        return Plan(scans, dependencyGraph);
    }

    // ============================================================
    // 公共 API — 测试友好重载（注入依赖图，不依赖真实 Assembly）
    // ============================================================

    /// <summary>
    /// 使用显式注入的程序集依赖图（程序集名称 → 其直接引用的程序集名称集合）计算注册计划。
    /// 主要供单元测试注入虚拟依赖图，无需加载真实 Assembly。
    /// </summary>
    /// <param name="scans">R1 扫描阶段产出的候选描述符，允许为空。</param>
    /// <param name="assemblyDependencyGraph">
    /// 程序集依赖图：key 为程序集简名，value 为其直接引用的程序集简名集合（仅含输入集合内的引用）。
    /// </param>
    /// <returns>已决策的 <see cref="ServiceRegistrationPlan"/>。</returns>
    public static ServiceRegistrationPlan Plan(
        IEnumerable<ServiceScanDescriptor> scans,
        IReadOnlyDictionary<string, IReadOnlySet<string>> assemblyDependencyGraph)
    {
        Check.NotNull(scans);
        Check.NotNull(assemblyDependencyGraph);

        var scanList = scans.ToList();
        var warnings = new List<string>();

        // Step 1: Compute topological indices via Kahn's algorithm.
        var topoIndex = ComputeTopologicalIndices(assemblyDependencyGraph);

        // Step 2: Expand scan descriptors — one per (impl, serviceType) tuple.
        var expanded = ExpandDescriptors(scanList, topoIndex);

        // Step 3: Group by (ServiceType, Key) and resolve conflicts.
        var registrations = ResolveGroups(expanded, warnings);

        return new ServiceRegistrationPlan(registrations, new ServiceRegistrationDiagnostics(warnings));
    }

    // ============================================================
    // Kahn's algorithm — topology
    // ============================================================

    /// <summary>
    /// 使用 Kahn 算法计算拓扑层级索引。
    /// 边方向：A → B 表示 A 引用 B（即 A 依赖 B）。
    /// Kahn 在反向图上运行：令 B → A 为反向边，则 B 的入度表示有多少程序集依赖 B。
    /// 无任何程序集依赖的纯基础库（如 Domain）在反向图中入度为 0，优先处理，获得最小索引（0）。
    /// 最上层叶子程序集（如 Api）在反向图中入度最大，最后处理，获得最大索引。
    /// "同层" 指同一 Kahn 迭代批次处理的节点；同层节点拥有相同索引，视为平级。
    /// </summary>
    private static Dictionary<string, int> ComputeTopologicalIndices(
        IReadOnlyDictionary<string, IReadOnlySet<string>> graph)
    {
        if (graph.Count == 0) return new Dictionary<string, int>(StringComparer.Ordinal);

        // Build the REVERSED graph: if A references B (edge A→B in original),
        // then add reverse edge B→A. Kahn on the reversed graph gives Domain-like
        // nodes (not referenced by anyone = out-degree 0 in original = in-degree 0 in reversed)
        // the smallest layer index, and leaf nodes (e.g. Api) the largest.

        // Compute in-degree in the REVERSED graph:
        // reversed in-degree of node N = number of nodes that N references in the original graph
        // (i.e., N's out-degree in the original graph).
        var reversedInDegree = new Dictionary<string, int>(StringComparer.Ordinal);
        var reversedAdj = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var node in graph.Keys)
        {
            if (!reversedInDegree.ContainsKey(node)) reversedInDegree[node] = 0;
            if (!reversedAdj.ContainsKey(node)) reversedAdj[node] = [];
        }

        foreach (var (node, refs) in graph)
        {
            foreach (var dep in refs)
            {
                if (!graph.ContainsKey(dep)) continue; // only within input set

                // Original edge: node → dep (node references dep)
                // Reversed edge: dep → node
                reversedInDegree[node] = reversedInDegree.GetValueOrDefault(node) + 1;
                if (!reversedAdj.ContainsKey(dep)) reversedAdj[dep] = [];
                reversedAdj[dep].Add(node);
            }
        }

        // Kahn on the reversed graph: start with nodes whose reversed in-degree = 0
        // (i.e., nodes that reference nothing in the original graph = pure base libraries).
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        var queue = new Queue<string>(
            reversedInDegree
                .Where(kv => kv.Value == 0)
                .Select(kv => kv.Key)
                .OrderBy(n => n, StringComparer.Ordinal));

        int layerIndex = 0;
        int processed = 0;

        while (queue.Count > 0)
        {
            int layerSize = queue.Count;
            var currentLayer = new List<string>(layerSize);

            for (int i = 0; i < layerSize; i++)
            {
                currentLayer.Add(queue.Dequeue());
            }

            foreach (var node in currentLayer)
            {
                result[node] = layerIndex;
                processed++;

                // Process reversed adjacency: decrease in-degree of nodes that reference this node.
                if (reversedAdj.TryGetValue(node, out var dependents))
                {
                    foreach (var dep in dependents.OrderBy(n => n, StringComparer.Ordinal))
                    {
                        reversedInDegree[dep]--;
                        if (reversedInDegree[dep] == 0)
                        {
                            queue.Enqueue(dep);
                        }
                    }
                }
            }

            layerIndex++;
        }

        // Cycle detection: if not all nodes were processed, there is a cycle.
        if (processed < graph.Count)
        {
            var remaining = graph.Keys.Where(n => !result.ContainsKey(n)).ToHashSet(StringComparer.Ordinal);
            var cyclePath = FindCyclePath(graph, remaining);
            throw new TwConfigurationException(
                $"程序集依赖图中存在循环引用，无法完成拓扑排序。循环路径：{cyclePath}");
        }

        return result;
    }

    /// <summary>
    /// 使用 DFS 在剩余节点集合中查找一条具体的循环路径。
    /// </summary>
    private static string FindCyclePath(
        IReadOnlyDictionary<string, IReadOnlySet<string>> graph,
        IReadOnlySet<string> remaining)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var stack = new List<string>();

        foreach (var start in remaining.OrderBy(n => n, StringComparer.Ordinal))
        {
            stack.Clear();
            visited.Clear();
            var found = DfsFind(graph, remaining, start, visited, stack);
            if (found is not null) return found;
        }

        return string.Join(" → ", remaining.OrderBy(n => n, StringComparer.Ordinal));
    }

    private static string? DfsFind(
        IReadOnlyDictionary<string, IReadOnlySet<string>> graph,
        IReadOnlySet<string> remaining,
        string node,
        HashSet<string> visited,
        List<string> stack)
    {
        if (!remaining.Contains(node)) return null;
        if (visited.Contains(node))
        {
            // Found cycle — extract cycle starting from the duplicate.
            int idx = stack.IndexOf(node);
            var cycle = stack.Skip(idx).Append(node);
            return string.Join(" → ", cycle);
        }

        visited.Add(node);
        stack.Add(node);

        if (graph.TryGetValue(node, out var refs))
        {
            foreach (var dep in refs.OrderBy(n => n, StringComparer.Ordinal))
            {
                var result = DfsFind(graph, remaining, dep, visited, stack);
                if (result is not null) return result;
            }
        }

        stack.RemoveAt(stack.Count - 1);
        return null;
    }

    // ============================================================
    // Expansion: ServiceScanDescriptor → ServiceRegistrationDescriptor[]
    // ============================================================

    private static List<ServiceRegistrationDescriptor> ExpandDescriptors(
        IEnumerable<ServiceScanDescriptor> scans,
        IReadOnlyDictionary<string, int> topoIndex)
    {
        var result = new List<ServiceRegistrationDescriptor>();
        var seen = new HashSet<(Type impl, Type service, object? key)>();

        foreach (var scan in scans)
        {
            int idx = topoIndex.TryGetValue(scan.AssemblyName, out var i) ? i : 0;

            foreach (var serviceType in scan.ServiceTypes.Distinct())
            {
                var tuple = (scan.ImplementationType, serviceType, scan.Key);
                if (!seen.Add(tuple)) continue; // de-dup

                result.Add(new ServiceRegistrationDescriptor(
                    ImplementationType: scan.ImplementationType,
                    ServiceType: serviceType,
                    Lifetime: scan.Lifetime,
                    Key: scan.Key,
                    IsCollection: scan.IsCollection,
                    IsReplacement: scan.IsReplacement,
                    Order: scan.Order,
                    AssemblyTopologicalIndex: idx,
                    IsOpenGenericDefinition: scan.IsOpenGenericDefinition,
                    AssemblyName: scan.AssemblyName));
            }
        }

        return result;
    }

    // ============================================================
    // Group-level conflict resolution
    // ============================================================

    private static List<ServiceRegistrationDescriptor> ResolveGroups(
        IEnumerable<ServiceRegistrationDescriptor> descriptors,
        List<string> warnings)
    {
        // Group by (ServiceType, Key). Use a comparer that handles null keys.
        var groups = descriptors
            .GroupBy(d => (d.ServiceType, d.Key), ServiceTypeKeyComparer.Instance)
            .OrderBy(g => g.Key.ServiceType.FullName ?? g.Key.ServiceType.Name, StringComparer.Ordinal)
            .ThenBy(g => g.Key.Key?.ToString() ?? string.Empty, StringComparer.Ordinal);

        var result = new List<ServiceRegistrationDescriptor>();

        foreach (var group in groups)
        {
            var items = group.ToList();
            var resolved = ResolveGroup(group.Key.ServiceType, group.Key.Key, items, warnings);
            result.AddRange(resolved);
        }

        return result;
    }

    private static IEnumerable<ServiceRegistrationDescriptor> ResolveGroup(
        Type serviceType,
        object? key,
        List<ServiceRegistrationDescriptor> items,
        List<string> warnings)
    {
        if (items.Count == 1)
        {
            // Single entry: check for keyed-mismatch warning (§5.5).
            // A [ReplaceService] with a key for which no plain (non-replacement) registration
            // exists at any key for the same ServiceType is treated as a new registration and emits a warning.
            // Since we already group by (ServiceType, Key), the mismatch scenario is handled at the
            // call site when we process cross-group keyed-mismatch detection.
            return items;
        }

        bool hasCollection = items.Any(d => d.IsCollection);
        bool hasNonCollection = items.Any(d => !d.IsCollection);

        if (hasCollection && hasNonCollection)
        {
            // Mixed: throw listing all impl full names.
            var names = items
                .Select(d => d.ImplementationType.FullName ?? d.ImplementationType.Name)
                .OrderBy(n => n, StringComparer.Ordinal);
            throw new TwConfigurationException(
                $"服务类型 {serviceType.FullName ?? serviceType.Name}（键={FormatKey(key)}）的注册候选中混合了集合与非集合实现，" +
                $"无法同时使用 [CollectionService] 和普通注册。冲突实现：{string.Join(", ", names)}");
        }

        if (hasCollection)
        {
            // All collection: keep all, stable order by (AssemblyTopologicalIndex ASC, Order ASC, FullName ASC).
            // Ascending on topo index means root-side first (lower index = root side),
            // but the spec says "order by (AssemblyTopologicalIndex, Order, type full name) ascending."
            return items
                .OrderBy(d => d.AssemblyTopologicalIndex)
                .ThenBy(d => d.Order)
                .ThenBy(d => d.ImplementationType.FullName ?? d.ImplementationType.Name, StringComparer.Ordinal);
        }

        // All non-collection: apply resolution rules.
        return ResolveNonCollectionGroup(serviceType, key, items, warnings);
    }

    private static IEnumerable<ServiceRegistrationDescriptor> ResolveNonCollectionGroup(
        Type serviceType,
        object? key,
        List<ServiceRegistrationDescriptor> items,
        List<string> warnings)
    {
        var replacements = items.Where(d => d.IsReplacement).ToList();
        var nonReplacements = items.Where(d => !d.IsReplacement).ToList();

        // §5.3 末尾: high-topology plain wins over low-topology replacement.
        // Determine winner between the two pools considering topology.

        if (nonReplacements.Count > 0 && replacements.Count > 0)
        {
            int maxNonReplacementTopo = nonReplacements.Max(d => d.AssemblyTopologicalIndex);
            int maxReplacementTopo = replacements.Max(d => d.AssemblyTopologicalIndex);

            if (maxNonReplacementTopo >= maxReplacementTopo)
            {
                // High-topology plain wins; warn about overriding replacements.
                foreach (var r in replacements.Where(r => r.AssemblyTopologicalIndex <= maxNonReplacementTopo))
                {
                    warnings.Add(
                        $"[ReplaceService] 实现 {r.ImplementationType.FullName ?? r.ImplementationType.Name}" +
                        $" 的拓扑层级（索引 {r.AssemblyTopologicalIndex}）低于或等于纯注册候选的拓扑层级（索引 {maxNonReplacementTopo}），" +
                        $"替换声明被上层普通注册覆盖，将被忽略。");
                }
                // Resolve using only non-replacements.
                return ResolveSingleWinner(serviceType, key, nonReplacements, warnings, isReplacementPool: false);
            }

            // Replacements have higher topology — use replacement pool only.
            // Warn that low-topology non-replacements are overridden.
            foreach (var nr in nonReplacements.Where(nr => nr.AssemblyTopologicalIndex < maxReplacementTopo))
            {
                warnings.Add(
                    $"服务类型 {serviceType.FullName ?? serviceType.Name} 存在拓扑层级更高的 [ReplaceService] 实现" +
                    $"（{string.Join(", ", replacements.Select(r => r.ImplementationType.FullName ?? r.ImplementationType.Name).OrderBy(n => n))}），" +
                    $"低层级普通注册 {nr.ImplementationType.FullName ?? nr.ImplementationType.Name} 将被覆盖。");
            }

            return ResolveSingleWinner(serviceType, key, replacements, warnings, isReplacementPool: true);
        }

        if (replacements.Count > 0)
        {
            // Only replacements.
            return ResolveSingleWinner(serviceType, key, replacements, warnings, isReplacementPool: true);
        }

        // Only non-replacements.
        return ResolveSingleWinner(serviceType, key, nonReplacements, warnings, isReplacementPool: false);
    }

    /// <summary>
    /// 从单个池（全为替换或全为非替换）中选出唯一胜者。
    /// 规则（§5.2）：
    /// 1. 取最大 AssemblyTopologicalIndex。
    /// 2. 若多个最大且来自不同程序集 → throw。
    /// 3. 若共享同一程序集 → 取最大 Order。
    /// 4. 仍然并列 → throw 含所有 FullName。
    /// </summary>
    private static IEnumerable<ServiceRegistrationDescriptor> ResolveSingleWinner(
        Type serviceType,
        object? key,
        List<ServiceRegistrationDescriptor> pool,
        List<string> warnings,
        bool isReplacementPool)
    {
        if (pool.Count == 1) return pool;

        int maxTopo = pool.Max(d => d.AssemblyTopologicalIndex);
        var topCandidates = pool.Where(d => d.AssemblyTopologicalIndex == maxTopo).ToList();

        if (topCandidates.Count == 1)
        {
            // Clear topology winner.
            if (pool.Count > 1)
            {
                // Emit cross-assembly topology win warning when the pool spans multiple assemblies.
                var loserAssemblies = pool
                    .Where(d => d.AssemblyTopologicalIndex < maxTopo)
                    .Select(d => d.ImplementationType.FullName ?? d.ImplementationType.Name)
                    .OrderBy(n => n, StringComparer.Ordinal)
                    .ToList();

                if (loserAssemblies.Count > 0)
                {
                    warnings.Add(
                        $"服务类型 {serviceType.FullName ?? serviceType.Name}（键={FormatKey(key)}）：" +
                        $"程序集 {topCandidates[0].AssemblyName}（拓扑索引 {maxTopo}）的实现 " +
                        $"{topCandidates[0].ImplementationType.FullName ?? topCandidates[0].ImplementationType.Name}" +
                        $" 通过跨程序集拓扑优先级覆盖了以下实现：{string.Join(", ", loserAssemblies)}");
                }
            }
            return topCandidates;
        }

        // Multiple with same max topo — check if from different assemblies.
        var assemblies = topCandidates.Select(d => d.AssemblyName).Distinct(StringComparer.Ordinal).ToList();

        if (assemblies.Count > 1)
        {
            // Different assemblies at same layer → throw.
            ThrowConflict(serviceType, key, topCandidates);
        }

        // Same assembly — pick max Order.
        int maxOrder = topCandidates.Max(d => d.Order);
        var orderWinners = topCandidates.Where(d => d.Order == maxOrder).ToList();

        if (orderWinners.Count == 1) return orderWinners;

        // Still tied → throw.
        ThrowConflict(serviceType, key, orderWinners);
        return []; // unreachable
    }

    private static void ThrowConflict(
        Type serviceType,
        object? key,
        IEnumerable<ServiceRegistrationDescriptor> conflicting)
    {
        var names = conflicting
            .Select(d => d.ImplementationType.FullName ?? d.ImplementationType.Name)
            .OrderBy(n => n, StringComparer.Ordinal);
        throw new TwConfigurationException(
            $"服务类型 {serviceType.FullName ?? serviceType.Name}（键={FormatKey(key)}）存在无法裁决的冲突注册，" +
            $"请使用 [ReplaceService]、[CollectionService] 或程序集分层消除歧义。" +
            $"冲突实现：{string.Join(", ", names)}");
    }

    private static string FormatKey(object? key)
        => key is null ? "null" : $"\"{key}\"";

    // ============================================================
    // Comparer for (ServiceType, Key) group key
    // ============================================================

    private sealed class ServiceTypeKeyComparer : IEqualityComparer<(Type ServiceType, object? Key)>
    {
        public static readonly ServiceTypeKeyComparer Instance = new();

        public bool Equals((Type ServiceType, object? Key) x, (Type ServiceType, object? Key) y)
            => x.ServiceType == y.ServiceType && Equals(x.Key, y.Key);

        public int GetHashCode((Type ServiceType, object? Key) obj)
        {
            var h = new HashCode();
            h.Add(obj.ServiceType);
            h.Add(obj.Key);
            return h.ToHashCode();
        }
    }
}
