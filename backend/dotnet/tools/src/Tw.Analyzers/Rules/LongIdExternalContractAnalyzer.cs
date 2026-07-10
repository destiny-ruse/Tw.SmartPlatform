namespace Tw.Analyzers.Rules;

/// <summary>
/// 分析长整型标识ExternalContract规则并报告 Roslyn 诊断
/// </summary>
public static class LongIdExternalContractAnalyzer
{
    /// <summary>
    /// 当前类型内部复用的Diagnostic标识常量值
    /// </summary>
    public const string DiagnosticId = "TWGOV006";
}
