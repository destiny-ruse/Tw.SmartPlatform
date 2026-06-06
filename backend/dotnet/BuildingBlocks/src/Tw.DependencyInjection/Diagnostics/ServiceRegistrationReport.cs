namespace Tw.DependencyInjection.Diagnostics;

/// <summary>
/// 服务注册规划诊断报告
/// </summary>
/// <remarks>
/// P1 仅填充程序集扫描与拓扑段落；候选服务、最终注册、仲裁结果、keyed 注册、
/// 跳过与冲突原因等段落由后续阶段（P2 起）在本类型上扩展。报告只承载摘要元数据，不输出敏感配置值。
/// </remarks>
public sealed class ServiceRegistrationReport
{
    /// <summary>初始化 <see cref="ServiceRegistrationReport"/> 的新实例（仅含程序集扫描与拓扑段落）</summary>
    /// <param name="scannedAssemblies">按拓扑顺序（被依赖在前）纳入扫描的程序集名</param>
    /// <param name="excludedAssemblies">被白/黑名单排除的程序集名</param>
    /// <param name="topology">程序集拓扑层级</param>
    public ServiceRegistrationReport(
        IReadOnlyList<string> scannedAssemblies,
        IReadOnlyList<string> excludedAssemblies,
        IReadOnlyList<AssemblyTopologyEntry> topology)
        : this(
            scannedAssemblies,
            excludedAssemblies,
            topology,
            candidates: [],
            registrations: [],
            supersededCandidates: [],
            skippedTypes: [],
            conflicts: [])
    {
    }

    /// <summary>初始化 <see cref="ServiceRegistrationReport"/> 的新实例（含完整注册规划诊断段落）</summary>
    /// <param name="scannedAssemblies">按拓扑顺序（被依赖在前）纳入扫描的程序集名</param>
    /// <param name="excludedAssemblies">被白/黑名单排除的程序集名</param>
    /// <param name="topology">程序集拓扑层级</param>
    /// <param name="candidates">服务注册候选列表</param>
    /// <param name="registrations">最终写入容器的服务注册列表</param>
    /// <param name="supersededCandidates">被单实现仲裁淘汰的候选列表</param>
    /// <param name="skippedTypes">扫描到但未参与普通服务注册的类型列表</param>
    /// <param name="conflicts">规划阶段检测到的冲突列表</param>
    public ServiceRegistrationReport(
        IReadOnlyList<string> scannedAssemblies,
        IReadOnlyList<string> excludedAssemblies,
        IReadOnlyList<AssemblyTopologyEntry> topology,
        IReadOnlyList<ServiceCandidateDiagnostic> candidates,
        IReadOnlyList<PlannedServiceRegistrationDiagnostic> registrations,
        IReadOnlyList<SupersededServiceCandidateDiagnostic> supersededCandidates,
        IReadOnlyList<SkippedServiceTypeDiagnostic> skippedTypes,
        IReadOnlyList<ServiceConflictDiagnostic> conflicts)
    {
        ScannedAssemblies = scannedAssemblies;
        ExcludedAssemblies = excludedAssemblies;
        Topology = topology;
        Candidates = candidates;
        Registrations = registrations;
        SupersededCandidates = supersededCandidates;
        SkippedTypes = skippedTypes;
        Conflicts = conflicts;
    }

    /// <summary>按拓扑顺序（被依赖在前）纳入扫描的程序集名</summary>
    public IReadOnlyList<string> ScannedAssemblies { get; }

    /// <summary>被白/黑名单排除的程序集名</summary>
    public IReadOnlyList<string> ExcludedAssemblies { get; }

    /// <summary>程序集拓扑层级</summary>
    public IReadOnlyList<AssemblyTopologyEntry> Topology { get; }

    /// <summary>服务注册候选</summary>
    public IReadOnlyList<ServiceCandidateDiagnostic> Candidates { get; }

    /// <summary>最终注册项</summary>
    public IReadOnlyList<PlannedServiceRegistrationDiagnostic> Registrations { get; }

    /// <summary>被仲裁淘汰的候选</summary>
    public IReadOnlyList<SupersededServiceCandidateDiagnostic> SupersededCandidates { get; }

    /// <summary>扫描到但跳过的类型</summary>
    public IReadOnlyList<SkippedServiceTypeDiagnostic> SkippedTypes { get; }

    /// <summary>规划阶段冲突</summary>
    public IReadOnlyList<ServiceConflictDiagnostic> Conflicts { get; }
}
