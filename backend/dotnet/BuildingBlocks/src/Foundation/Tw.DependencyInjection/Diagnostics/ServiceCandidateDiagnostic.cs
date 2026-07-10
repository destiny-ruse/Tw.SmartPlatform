using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Diagnostics;

/// <summary>
/// 服务注册候选诊断项
/// </summary>
/// <param name="ImplementationTypeName">实现类型全名</param>
/// <param name="ServiceTypeName">服务类型全名</param>
/// <param name="Key">Keyed 服务的键；非 keyed 服务为 <see langword="null"/></param>
/// <param name="Lifetime">服务生命周期</param>
/// <param name="AssemblyName">实现类型所在程序集名</param>
/// <param name="FinalPriority">仲裁后的最终优先级</param>
/// <param name="Status">
/// 候选状态；合法值为 <c>selected</c>、<c>superseded</c>、<c>skipped</c>，大小写敏感，
/// 由注册规划器统一写入，不接受其他字符串。
/// </param>
public sealed record ServiceCandidateDiagnostic(
    string ImplementationTypeName,
    string ServiceTypeName,
    object? Key,
    DependencyLifetime Lifetime,
    string AssemblyName,
    long FinalPriority,
    string Status);
