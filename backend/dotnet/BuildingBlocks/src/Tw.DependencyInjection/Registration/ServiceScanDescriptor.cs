using Microsoft.Extensions.DependencyInjection;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 扫描阶段产出的服务候选记录。
/// R2 计划阶段将进一步展开为按 (ServiceType, Key) 分组的注册描述符。
/// </summary>
/// <param name="ImplementationType">实现类型。</param>
/// <param name="ServiceTypes">本实现对外暴露的服务类型列表，由曝光规则推导或显式声明。</param>
/// <param name="Lifetime">服务生命周期，由生命周期标记接口决定。</param>
/// <param name="Key">命名键；为 <see langword="null"/> 时表示无键注册。</param>
/// <param name="IsCollection">是否以集合追加模式注册。</param>
/// <param name="IsReplacement">是否替换已注册的同一服务接口实现。</param>
/// <param name="Order">注册顺序，适用于集合或替换场景。</param>
/// <param name="IsOpenGenericDefinition">实现类型是否为开放泛型定义。</param>
/// <param name="AssemblyName">实现类型所在程序集的简单名称。</param>
public sealed record ServiceScanDescriptor(
    Type ImplementationType,
    IReadOnlyList<Type> ServiceTypes,
    ServiceLifetime Lifetime,
    object? Key,
    bool IsCollection,
    bool IsReplacement,
    int Order,
    bool IsOpenGenericDefinition,
    string AssemblyName);
