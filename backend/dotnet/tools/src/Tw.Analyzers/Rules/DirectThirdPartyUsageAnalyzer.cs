namespace Tw.Analyzers.Rules;

/// <summary>
/// 分析Direct第三个PartyUsage规则并报告 Roslyn 诊断
/// </summary>
public static class DirectThirdPartyUsageAnalyzer
{
    /// <summary>
    /// 当前类型内部复用的Diagnostic标识常量值
    /// </summary>
    public const string DiagnosticId = "TWGOV004";
}
