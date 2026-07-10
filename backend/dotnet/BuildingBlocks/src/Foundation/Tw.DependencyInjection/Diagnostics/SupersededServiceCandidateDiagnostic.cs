namespace Tw.DependencyInjection.Diagnostics;

/// <summary>
/// 被单实现仲裁淘汰的候选诊断项
/// </summary>
/// <param name="ServiceTypeName">服务类型全名</param>
/// <param name="ImplementationTypeName">被淘汰的实现类型全名</param>
/// <param name="Key">Keyed 服务的键；非 keyed 服务为 <see langword="null"/></param>
/// <param name="SelectedImplementationTypeName">最终被选中的实现类型全名</param>
/// <param name="FinalPriority">被淘汰候选的最终优先级</param>
/// <param name="SelectedFinalPriority">被选中实现的最终优先级</param>
public sealed record SupersededServiceCandidateDiagnostic(
    string ServiceTypeName,
    string ImplementationTypeName,
    object? Key,
    string SelectedImplementationTypeName,
    long FinalPriority,
    long SelectedFinalPriority);
