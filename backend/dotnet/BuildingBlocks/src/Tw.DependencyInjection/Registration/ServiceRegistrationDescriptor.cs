using Microsoft.Extensions.DependencyInjection;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 计划阶段产出的单条服务注册记录，每条记录对应一个 (实现类型, 服务类型) 元组。
/// R1 扫描阶段产出 <see cref="ServiceScanDescriptor"/>（一个实现多个服务类型），
/// R2 计划阶段将其展开为本记录（每条对应一个服务类型）。
/// </summary>
/// <param name="ImplementationType">实现类型。</param>
/// <param name="ServiceType">本条注册对外暴露的单一服务类型。</param>
/// <param name="Lifetime">服务生命周期。</param>
/// <param name="Key">命名键；为 <see langword="null"/> 时表示无键注册。</param>
/// <param name="IsCollection">是否以集合追加模式注册。</param>
/// <param name="IsReplacement">是否替换已注册的同一服务接口实现。</param>
/// <param name="Order">注册顺序，适用于集合或替换场景。</param>
/// <param name="AssemblyTopologicalIndex">
/// 所属程序集在拓扑图中的层级索引，叶子节点（上层程序集）拥有最大索引值，优先级最高。
/// </param>
/// <param name="IsOpenGenericDefinition">实现类型是否为开放泛型定义。</param>
/// <param name="AssemblyName">实现类型所在程序集的简单名称。</param>
public sealed record ServiceRegistrationDescriptor(
    Type ImplementationType,
    Type ServiceType,
    ServiceLifetime Lifetime,
    object? Key,
    bool IsCollection,
    bool IsReplacement,
    int Order,
    int AssemblyTopologicalIndex,
    bool IsOpenGenericDefinition,
    string AssemblyName);
