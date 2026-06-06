using System.Reflection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 服务注册规划器：遍历候选类型、解析暴露契约与生命周期、按优先级执行单实现仲裁，产出最终注册列表与诊断报告
/// </summary>
internal static class ServiceRegistrationPlanner
{
    /// <summary>
    /// 执行注册规划，返回包含最终候选列表与诊断报告的 <see cref="ServiceRegistrationPlan"/>
    /// </summary>
    /// <param name="assemblies">参与规划的程序集列表（已按拓扑序排列，被依赖在前）</param>
    /// <param name="typesByAssemblyName">各程序集应参与规划的类型集合，key 为程序集名</param>
    /// <param name="topologyLevelsByAssemblyName">各程序集的拓扑层级，key 为程序集名；缺失时默认 0</param>
    /// <param name="reachabilityGraph">程序集有向可达性图，用于平级仲裁失败检测</param>
    /// <param name="options">注册选项，包含程序集级显式优先级配置</param>
    /// <returns>规划结果，含仲裁后的注册候选列表与完整诊断报告</returns>
    /// <exception cref="ArgumentNullException">任意参数为 <c>null</c> 时抛出</exception>
    /// <exception cref="Tw.DependencyInjection.ServiceRegistrationException">
    /// 同一服务契约存在无法仲裁的候选（最终优先级相同、或位于平级程序集未显式设定优先级）时抛出
    /// </exception>
    public static ServiceRegistrationPlan Plan(
        IReadOnlyList<Assembly> assemblies,
        IReadOnlyDictionary<string, IReadOnlyList<Type>> typesByAssemblyName,
        IReadOnlyDictionary<string, int> topologyLevelsByAssemblyName,
        AssemblyReachabilityGraph reachabilityGraph,
        ServiceRegistrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(typesByAssemblyName);
        ArgumentNullException.ThrowIfNull(topologyLevelsByAssemblyName);
        ArgumentNullException.ThrowIfNull(reachabilityGraph);
        ArgumentNullException.ThrowIfNull(options);

        // ── 阶段一：生成候选（稳定顺序）──────────────────────────────────────
        var candidates = new List<ServiceCandidate>();
        var skipped = new List<SkippedServiceTypeDiagnostic>();
        int discoveryOrder = 0;

        // 按 assemblies 给定的拓扑顺序逐个程序集处理
        foreach (var assembly in assemblies)
        {
            var assemblyName = assembly.GetName().Name!;

            // 取该程序集的类型列表；按 FullName（或 Name）升序排序，保证稳定输出
            var types = typesByAssemblyName.TryGetValue(assemblyName, out var typeList)
                ? typeList
                : [];

            var sortedTypes = types
                .OrderBy(t => t.FullName ?? t.Name, StringComparer.Ordinal)
                .ToList();

            foreach (var type in sortedTypes)
            {
                // 每个 type 占一个 discoveryOrder，先取后自增，无论是否成为候选
                int thisDiscoveryOrder = discoveryOrder++;

                // 未声明参与注册的类型直接静默跳过（不记 skipped）
                if (!ServiceTypeInspector.IsRegistrationParticipant(type))
                {
                    continue;
                }

                // 接口、抽象类、Options 等应跳过普通注册
                if (ServiceTypeInspector.ShouldSkipOrdinaryRegistration(type, out var skipReason))
                {
                    skipped.Add(new SkippedServiceTypeDiagnostic(type.FullName ?? type.Name, skipReason));
                    continue;
                }

                // 无法解析生命周期（多标记/未声明）则记 skipped 跳过，不抛异常
                if (!ServiceTypeInspector.TryResolveLifetime(type, out var lifetime, out var lifeReason))
                {
                    skipped.Add(new SkippedServiceTypeDiagnostic(
                        type.FullName ?? type.Name,
                        lifeReason ?? "无法解析生命周期"));
                    continue;
                }

                // 解析优先级与拓扑层级
                var assemblyPriority = ServicePriorityResolver.ResolveAssemblyPriority(assembly, options);
                var typePriority = ServicePriorityResolver.ResolveTypePriority(type);
                var topologyLevel = topologyLevelsByAssemblyName.TryGetValue(assemblyName, out var lvl) ? lvl : 0;
                var finalPriority = ServicePriorityResolver.CalculateFinalPriority(topologyLevel, assemblyPriority, typePriority);

                // 对该实现类型的每个暴露契约生成一个候选
                var exposures = ServiceExposureResolver.Resolve(type);
                foreach (var exposure in exposures)
                {
                    candidates.Add(new ServiceCandidate(
                        ServiceType: exposure.ServiceType,
                        ImplementationType: type,
                        Key: exposure.Key,
                        Lifetime: lifetime,
                        AssemblyName: assemblyName,
                        TopologyLevel: topologyLevel,
                        AssemblyPriority: assemblyPriority,
                        TypePriority: typePriority,
                        FinalPriority: finalPriority,
                        DiscoveryOrder: thisDiscoveryOrder));
                }
            }
        }

        // ── 阶段二：分组仲裁 ──────────────────────────────────────────────────
        var registrations = new List<ServiceCandidate>();
        var supersededCandidates = new List<ServiceCandidate>();

        // 按 (ServiceType, Key) 分组；ValueTuple 默认相等语义满足 string key 按值去重、null 正确处理
        var groups = candidates.GroupBy(c => (c.ServiceType, c.Key));

        foreach (var group in groups)
        {
            var (winner, superseded) = ArbitrateGroup(group, reachabilityGraph);
            registrations.Add(winner);
            supersededCandidates.AddRange(superseded);
        }

        // ── 阶段三：构造诊断 ──────────────────────────────────────────────────

        // 构建 winner 集合的快速查找：(ServiceType, Key, ImplementationType) → winner
        var winnerLookup = registrations.ToHashSet();

        // 候选诊断：按 DiscoveryOrder 再按 ServiceType 全名排序，保证稳定输出
        var candidateDiagnostics = candidates
            .OrderBy(c => c.DiscoveryOrder)
            .ThenBy(c => c.ServiceType.FullName ?? c.ServiceType.Name, StringComparer.Ordinal)
            .Select(c =>
            {
                // 判断该候选是否为其分组的 winner
                var status = winnerLookup.Contains(c) ? "selected" : "superseded";
                return new ServiceCandidateDiagnostic(
                    ImplementationTypeName: c.ImplementationType.FullName ?? c.ImplementationType.Name,
                    ServiceTypeName: c.ServiceType.FullName ?? c.ServiceType.Name,
                    Key: c.Key,
                    Lifetime: c.Lifetime,
                    AssemblyName: c.AssemblyName,
                    FinalPriority: c.FinalPriority,
                    Status: status);
            })
            .ToList();

        // 注册诊断
        var registrationDiagnostics = registrations
            .Select(w => new PlannedServiceRegistrationDiagnostic(
                ServiceTypeName: w.ServiceType.FullName ?? w.ServiceType.Name,
                ImplementationTypeName: w.ImplementationType.FullName ?? w.ImplementationType.Name,
                Key: w.Key,
                Lifetime: w.Lifetime,
                FinalPriority: w.FinalPriority))
            .ToList();

        // 落选候选诊断：需要同时找到对应分组的 winner
        var supersededDiagnostics = new List<SupersededServiceCandidateDiagnostic>();
        foreach (var s in supersededCandidates)
        {
            // 在 registrations 中找同一 (ServiceType, Key) 分组的 winner
            var winner = registrations.First(w =>
                w.ServiceType == s.ServiceType &&
                Equals(w.Key, s.Key));

            supersededDiagnostics.Add(new SupersededServiceCandidateDiagnostic(
                ServiceTypeName: s.ServiceType.FullName ?? s.ServiceType.Name,
                ImplementationTypeName: s.ImplementationType.FullName ?? s.ImplementationType.Name,
                Key: s.Key,
                SelectedImplementationTypeName: winner.ImplementationType.FullName ?? winner.ImplementationType.Name,
                FinalPriority: s.FinalPriority,
                SelectedFinalPriority: winner.FinalPriority));
        }

        var report = new ServiceRegistrationReport(
            scannedAssemblies: assemblies.Select(a => a.GetName().Name ?? string.Empty).ToList(),
            excludedAssemblies: [],
            topology: topologyLevelsByAssemblyName
                .Select(kv => new AssemblyTopologyEntry(kv.Key, kv.Value))
                .OrderBy(e => e.Level)
                .ThenBy(e => e.AssemblyName, StringComparer.Ordinal)
                .ToList(),
            candidates: candidateDiagnostics,
            registrations: registrationDiagnostics,
            supersededCandidates: supersededDiagnostics,
            skippedTypes: skipped,
            conflicts: []);   // P2 通过抛异常表达冲突，conflicts 段落留空

        return new ServiceRegistrationPlan(registrations, report);
    }

    /// <summary>
    /// 对单个 (ServiceType, Key) 分组执行单实现仲裁，返回获胜候选及所有落选候选
    /// </summary>
    /// <param name="group">同一服务契约的所有候选</param>
    /// <param name="reachabilityGraph">程序集有向可达性图</param>
    /// <returns>(<paramref name="winner"/> 获胜候选，落选候选列表)</returns>
    /// <exception cref="Tw.DependencyInjection.ServiceRegistrationException">
    /// 候选位于平级程序集未显式仲裁（拓扑层级不同但彼此不可达、程序集与类型优先级均相同）时抛出；
    /// 或最终优先级相同无法仲裁时抛出。
    /// </exception>
    private static (ServiceCandidate winner, IReadOnlyList<ServiceCandidate> superseded)
        ArbitrateGroup(
            IGrouping<(Type ServiceType, object? Key), ServiceCandidate> group,
            AssemblyReachabilityGraph reachabilityGraph)
    {
        // 按 FinalPriority 降序、再按 DiscoveryOrder 升序排列，保证稳定输出
        var ordered = group
            .OrderByDescending(c => c.FinalPriority)
            .ThenBy(c => c.DiscoveryOrder)
            .ToList();

        var winner = ordered[0];

        // 只有一个候选时无需仲裁
        if (ordered.Count == 1)
        {
            return (winner, []);
        }

        var runnerUp = ordered[1];

        // ── 失败条件 1：平级程序集仲裁失败 ──────────────────────────────────
        // 条件：两者的程序集优先级与类型优先级完全相同，但拓扑层级不同（即 FinalPriority 差异
        // 仅来自拓扑层级），且两个程序集彼此不可达（平级）。
        // 在这种情况下，拓扑层级区分了优先级，但拓扑并非用户显式设定的仲裁手段，
        // 必须要求用户通过程序集或类型优先级显式仲裁。
        var onlyTopologyDiffers = winner.AssemblyPriority == runnerUp.AssemblyPriority
            && winner.TypePriority == runnerUp.TypePriority
            && winner.TopologyLevel != runnerUp.TopologyLevel;

        var assembliesArePeers = !reachabilityGraph.CanReach(winner.AssemblyName, runnerUp.AssemblyName)
            && !reachabilityGraph.CanReach(runnerUp.AssemblyName, winner.AssemblyName);

        if (onlyTopologyDiffers && assembliesArePeers)
        {
            throw new Tw.DependencyInjection.ServiceRegistrationException(
                $"服务契约 {winner.ServiceType.FullName} 的候选位于平级程序集，必须通过程序集或类型优先级显式仲裁");
        }

        // ── 失败条件 2：最终优先级相同，无法仲裁 ─────────────────────────────
        // FinalPriority 完全相同时（同程序集同拓扑层级同两级优先级），无法确定唯一实现，抛出异常。
        // 注意：该分支在 winner 与 runnerUp 来自同一程序集、TopologyLevel 相同时触发，
        // 不会被上方平级失败条件（onlyTopologyDiffers 为 false）误拦截。
        if (winner.FinalPriority == runnerUp.FinalPriority)
        {
            throw new Tw.DependencyInjection.ServiceRegistrationException(
                $"服务契约 {winner.ServiceType.FullName} 的候选最终优先级相同，无法仲裁唯一实现");
        }

        // winner 合法产生，其余均为落选候选
        var superseded = ordered.Skip(1).ToList();
        return (winner, superseded);
    }
}
