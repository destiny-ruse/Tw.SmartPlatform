namespace Tw.DependencyInjection.Diagnostics;

/// <summary>
/// 规划阶段检测到的服务注册冲突诊断项
/// </summary>
/// <param name="ServiceTypeName">冲突服务类型全名</param>
/// <param name="Key">Keyed 服务的键；非 keyed 服务为 <see langword="null"/></param>
/// <param name="ImplementationTypeNames">冲突的实现类型全名列表</param>
/// <param name="Reason">冲突原因描述</param>
public sealed record ServiceConflictDiagnostic(
    string ServiceTypeName,
    object? Key,
    IReadOnlyList<string> ImplementationTypeNames,
    string Reason);
