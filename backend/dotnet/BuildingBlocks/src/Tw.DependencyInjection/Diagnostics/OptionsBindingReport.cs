namespace Tw.DependencyInjection.Diagnostics;

/// <summary>
/// Options 自动装载诊断报告
/// </summary>
public sealed class OptionsBindingReport
{
    /// <summary>
    /// 初始化 Options 绑定诊断报告
    /// </summary>
    /// <param name="items">绑定诊断项</param>
    public OptionsBindingReport(IReadOnlyList<OptionsBindingDiagnostic> items)
    {
        Items = items;
    }

    /// <summary>
    /// Options 绑定诊断项
    /// </summary>
    public IReadOnlyList<OptionsBindingDiagnostic> Items { get; }
}
