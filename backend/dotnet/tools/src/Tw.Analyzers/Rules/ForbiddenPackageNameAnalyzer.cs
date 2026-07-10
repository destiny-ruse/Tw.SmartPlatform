namespace Tw.Analyzers.Rules;

/// <summary>
/// 分析禁止Package名称规则并报告 Roslyn 诊断
/// </summary>
public static class ForbiddenPackageNameAnalyzer
{
    /// <summary>
    /// 当前类型内部复用的Diagnostic标识常量值
    /// </summary>
    public const string DiagnosticId = "TWGOV002";
}
