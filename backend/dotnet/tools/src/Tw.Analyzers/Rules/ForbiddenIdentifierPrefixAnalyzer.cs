using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tw.Analyzers.Rules;

/// <summary>
/// 分析禁止标识符前缀规则并报告 Roslyn 诊断
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForbiddenIdentifierPrefixAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// 当前类型内部复用的Diagnostic标识常量值
    /// </summary>
    public const string DiagnosticId = "TWGOV001";

    /// <summary>
    /// 保存当前类型处理流程依赖的禁止Prefixes
    /// </summary>
    private static readonly ImmutableArray<string> ForbiddenPrefixes = ImmutableArray.Create("Tw", "Abp", "Furion");

    /// <summary>
    /// 保存当前类型处理流程依赖的Rule
    /// </summary>
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Avoid framework-owned identifier prefixes",
        "Identifier '{0}' must not use framework-owned prefix '{1}'",
        "Governance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// 创建在当前对象中的业务含义
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <summary>
    /// 初始化分析器的并发执行和语法节点注册
    /// </summary>
    /// <param name="context">当前调用携带的上下文信息</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    /// <summary>
    /// 分析命名类型并报告匹配的诊断
    /// </summary>
    /// <param name="context">当前调用携带的上下文信息</param>
    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.Name.Equals("TwException", StringComparison.Ordinal))
        {
            return;
        }

        foreach (var prefix in ForbiddenPrefixes)
        {
            if (symbol.Name.StartsWith(prefix, StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    symbol.Locations.FirstOrDefault(),
                    symbol.Name,
                    prefix));
                return;
            }
        }
    }
}
