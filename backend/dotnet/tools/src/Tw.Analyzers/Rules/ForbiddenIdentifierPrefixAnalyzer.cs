using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tw.Analyzers.Rules;

/// <summary>表示 ForbiddenIdentifierPrefixAnalyzer 类型</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForbiddenIdentifierPrefixAnalyzer : DiagnosticAnalyzer
{
    /// <summary>表示 DiagnosticId 常量</summary>
    public const string DiagnosticId = "TWGOV001";

    /// <summary>表示 ForbiddenPrefixes 字段</summary>
    private static readonly ImmutableArray<string> ForbiddenPrefixes = ImmutableArray.Create("Tw", "Abp", "Furion");

    /// <summary>表示 Rule 字段</summary>
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Avoid framework-owned identifier prefixes",
        "Identifier '{0}' must not use framework-owned prefix '{1}'",
        "Governance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>表示 SupportedDiagnostics 属性</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <summary>执行 Initialize 操作</summary>
    /// <param name="context">context 参数</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    /// <summary>执行 AnalyzeNamedType 操作</summary>
    /// <param name="context">context 参数</param>
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
