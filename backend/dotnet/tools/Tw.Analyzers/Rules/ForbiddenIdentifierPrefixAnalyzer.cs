using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tw.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForbiddenIdentifierPrefixAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TWGOV001";

    private static readonly ImmutableArray<string> ForbiddenPrefixes = ImmutableArray.Create("Tw", "Abp", "Furion");

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Avoid framework-owned identifier prefixes",
        "Identifier '{0}' must not use framework-owned prefix '{1}'",
        "Governance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

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
