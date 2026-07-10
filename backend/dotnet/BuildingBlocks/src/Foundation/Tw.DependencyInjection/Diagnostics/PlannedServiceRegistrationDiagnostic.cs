using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Diagnostics;

/// <summary>
/// 最终写入容器的服务注册诊断项
/// </summary>
/// <param name="ServiceTypeName">服务类型全名</param>
/// <param name="ImplementationTypeName">实现类型全名</param>
/// <param name="Key">Keyed 服务的键；非 keyed 服务为 <see langword="null"/></param>
/// <param name="Lifetime">服务生命周期</param>
/// <param name="FinalPriority">仲裁后的最终优先级</param>
public sealed record PlannedServiceRegistrationDiagnostic(
    string ServiceTypeName,
    string ImplementationTypeName,
    object? Key,
    DependencyLifetime Lifetime,
    long FinalPriority);
