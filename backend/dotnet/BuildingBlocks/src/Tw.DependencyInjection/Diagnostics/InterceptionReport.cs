namespace Tw.DependencyInjection.Diagnostics;

/// <summary>
/// AOP 拦截承载诊断报告
/// </summary>
public sealed class InterceptionReport
{
    /// <summary>
    /// 初始化 AOP 拦截承载诊断报告
    /// </summary>
    /// <param name="items">方法级拦截承载诊断项列表</param>
    public InterceptionReport(IReadOnlyList<InterceptionDiagnostic> items) => Items = items;

    /// <summary>
    /// 方法级拦截承载诊断项列表
    /// </summary>
    public IReadOnlyList<InterceptionDiagnostic> Items { get; }
}
