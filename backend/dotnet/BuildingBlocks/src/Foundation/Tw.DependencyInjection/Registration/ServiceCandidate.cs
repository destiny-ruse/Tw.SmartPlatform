using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 在注册规划阶段描述一个服务注册候选——即某实现类型对某契约类型（含可选 key）的一次暴露关系及其优先级信息
/// </summary>
/// <param name="ServiceType">服务契约类型</param>
/// <param name="ImplementationType">实现类型</param>
/// <param name="Key">Keyed 服务的键；非 keyed 服务为 <c>null</c></param>
/// <param name="Lifetime">服务生命周期</param>
/// <param name="AssemblyName">实现类型所在程序集名</param>
/// <param name="TopologyLevel">程序集在拓扑排序中的层级；0 为最底层</param>
/// <param name="AssemblyPriority">程序集级显式优先级</param>
/// <param name="TypePriority">类型级显式优先级</param>
/// <param name="FinalPriority">合成最终优先级；值越大越优先</param>
/// <param name="DiscoveryOrder">扫描时的发现顺序；用于最终优先级相同时保持稳定排序</param>
internal sealed record ServiceCandidate(
    Type ServiceType,
    Type ImplementationType,
    object? Key,
    DependencyLifetime Lifetime,
    string AssemblyName,
    int TopologyLevel,
    int AssemblyPriority,
    int TypePriority,
    long FinalPriority,
    int DiscoveryOrder);
