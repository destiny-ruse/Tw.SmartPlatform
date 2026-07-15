using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tw.Analyzers.Rules;

/// <summary>
/// 对声明标识符中的框架品牌分段实施治理诊断
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForbiddenBrandIdentifierAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// 品牌标识符分段治理诊断的稳定标识
    /// </summary>
    public const string DiagnosticId = "TWGOV001";

    /// <summary>
    /// 不能作为业务声明标识符分段使用的框架品牌名称
    /// </summary>
    private static readonly ImmutableArray<string> ForbiddenBrandSegments = ImmutableArray.Create("Tw", "Abp", "Furion");

    /// <summary>
    /// 描述品牌标识符分段治理违规的 Roslyn 诊断定义
    /// </summary>
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Avoid framework-owned identifier segments",
        "Identifier '{0}' must not contain framework-owned brand segment '{1}'",
        "Governance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// 提供此分析器能够报告的治理诊断定义
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <summary>
    /// 注册仅针对源代码声明符号的并发分析操作
    /// </summary>
    /// <param name="context">用于注册分析回调的 Roslyn 上下文</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(
            AnalyzeDeclaration,
            SymbolKind.NamedType,
            SymbolKind.Method,
            SymbolKind.Property,
            SymbolKind.Field,
            SymbolKind.Event,
            SymbolKind.Parameter);
        context.RegisterSyntaxNodeAction(
            AnalyzeLocalDeclaration,
            SyntaxKind.VariableDeclarator,
            SyntaxKind.ForEachStatement,
            SyntaxKind.CatchDeclaration,
            SyntaxKind.SingleVariableDesignation);
        context.RegisterSyntaxNodeAction(
            AnalyzeAdditionalDeclaration,
            SyntaxKind.TypeParameter,
            SyntaxKind.UsingDirective,
            SyntaxKind.LabeledStatement,
            SyntaxKind.FromClause,
            SyntaxKind.LetClause,
            SyntaxKind.JoinClause,
            SyntaxKind.JoinIntoClause,
            SyntaxKind.QueryContinuation);
    }

    /// <summary>
    /// 检查单个源代码声明符号是否包含受治理的品牌分段
    /// </summary>
    /// <param name="context">包含待检查声明符号和诊断报告入口的 Roslyn 上下文</param>
    private static void AnalyzeDeclaration(SymbolAnalysisContext context)
    {
        var diagnostic = CreateDiagnostic(context.Symbol, context.Compilation);
        if (diagnostic is not null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// 检查所有局部变量声明语法并仅分析其声明的局部变量符号
    /// </summary>
    /// <param name="context">包含局部变量声明节点和语义模型的 Roslyn 上下文</param>
    private static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context)
    {
        var localSymbol = context.Node switch
        {
            VariableDeclaratorSyntax declarator => context.SemanticModel.GetDeclaredSymbol(declarator, context.CancellationToken),
            ForEachStatementSyntax forEachStatement => context.SemanticModel.GetDeclaredSymbol(forEachStatement, context.CancellationToken),
            CatchDeclarationSyntax catchDeclaration => context.SemanticModel.GetDeclaredSymbol(catchDeclaration, context.CancellationToken),
            SingleVariableDesignationSyntax designation => context.SemanticModel.GetDeclaredSymbol(designation, context.CancellationToken),
            _ => null
        };

        if (localSymbol is not ILocalSymbol)
        {
            return;
        }

        var diagnostic = CreateDiagnostic(localSymbol, context.SemanticModel.Compilation);
        if (diagnostic is not null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// 检查类型参数、别名、标签和查询范围变量等补充声明语法
    /// </summary>
    /// <param name="context">包含补充声明节点和语义模型的 Roslyn 上下文</param>
    private static void AnalyzeAdditionalDeclaration(SyntaxNodeAnalysisContext context)
    {
        ISymbol? symbol = context.Node switch
        {
            TypeParameterSyntax typeParameter => context.SemanticModel.GetDeclaredSymbol(typeParameter, context.CancellationToken),
            UsingDirectiveSyntax usingDirective when usingDirective.Alias is not null => context.SemanticModel.GetDeclaredSymbol(usingDirective, context.CancellationToken),
            LabeledStatementSyntax labeledStatement => context.SemanticModel.GetDeclaredSymbol(labeledStatement, context.CancellationToken),
            FromClauseSyntax fromClause => context.SemanticModel.GetDeclaredSymbol(fromClause, context.CancellationToken),
            LetClauseSyntax letClause => context.SemanticModel.GetDeclaredSymbol(letClause, context.CancellationToken),
            JoinClauseSyntax joinClause => context.SemanticModel.GetDeclaredSymbol(joinClause, context.CancellationToken),
            JoinIntoClauseSyntax joinIntoClause => context.SemanticModel.GetDeclaredSymbol(joinIntoClause, context.CancellationToken),
            QueryContinuationSyntax queryContinuation => context.SemanticModel.GetDeclaredSymbol(queryContinuation, context.CancellationToken),
            _ => null
        };

        if (symbol is null)
        {
            return;
        }

        var diagnostic = CreateDiagnostic(symbol, context.SemanticModel.Compilation);
        if (diagnostic is not null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// 为需要治理的声明符号创建诊断，或在符号不适用时返回 null
    /// </summary>
    /// <param name="symbol">准备检查品牌分段的声明符号</param>
    /// <param name="compilation">用于解析批准异常基类型的当前编译</param>
    /// <returns>声明符号违反品牌分段规则时返回对应诊断</returns>
    private static Diagnostic? CreateDiagnostic(ISymbol symbol, Compilation compilation)
    {
        if (symbol.IsImplicitlyDeclared ||
            symbol is IMethodSymbol { AssociatedSymbol: not null } ||
            IsApprovedExceptionType(symbol, compilation))
        {
            return null;
        }

        if (!TryGetForbiddenBrandSegment(symbol.Name, out var forbiddenSegment))
        {
            return null;
        }

        var location = symbol.Locations.FirstOrDefault(candidate => candidate.IsInSource);
        if (location is null)
        {
            return null;
        }

        return Diagnostic.Create(Rule, location, symbol.Name, forbiddenSegment);
    }

    /// <summary>
    /// 判断符号是否为治理规则唯一批准保留品牌分段的异常类型
    /// </summary>
    /// <param name="symbol">准备判断例外条件的声明符号</param>
    /// <param name="compilation">用于解析 System.Exception 元数据类型的当前编译</param>
    /// <returns>仅当符号满足批准异常类型的全部语义条件时返回 true</returns>
    private static bool IsApprovedExceptionType(ISymbol symbol, Compilation compilation)
    {
        if (symbol is not INamedTypeSymbol namedType ||
            namedType.Arity != 0 ||
            namedType.ContainingType is not null ||
            !namedType.Name.Equals("TwException", StringComparison.Ordinal) ||
            !namedType.ContainingNamespace.ToDisplayString().Equals("Tw.Exceptions", StringComparison.Ordinal) ||
            !namedType.ContainingAssembly.Name.Equals("Tw.Core", StringComparison.Ordinal))
        {
            return false;
        }

        var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
        if (exceptionType is null)
        {
            return false;
        }

        for (var baseType = namedType.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, exceptionType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 在标识符的语义分段中查找受治理的品牌名称
    /// </summary>
    /// <param name="identifier">准备按命名规则切分的声明标识符</param>
    /// <param name="forbiddenSegment">命中时返回标准化的受治理品牌名称</param>
    /// <returns>标识符存在受治理品牌分段时返回 true</returns>
    private static bool TryGetForbiddenBrandSegment(string identifier, out string forbiddenSegment)
    {
        var tokenStart = 0;

        for (var index = 0; index <= identifier.Length; index++)
        {
            var isEndOfIdentifier = index == identifier.Length;
            var isUnderscore = !isEndOfIdentifier && identifier[index] == '_';
            var startsNewToken = !isEndOfIdentifier && !isUnderscore && StartsNewToken(identifier, index);
            if (!isEndOfIdentifier && !isUnderscore && !startsNewToken)
            {
                continue;
            }

            if (TryMatchForbiddenBrandSegment(identifier, tokenStart, index - tokenStart, out forbiddenSegment))
            {
                return true;
            }

            tokenStart = isUnderscore ? index + 1 : index;
        }

        forbiddenSegment = string.Empty;
        return false;
    }

    /// <summary>
    /// 判断当前位置是否由大小写转换形成新的命名分段
    /// </summary>
    /// <param name="identifier">准备判断分段边界的声明标识符</param>
    /// <param name="index">当前字符在标识符中的索引</param>
    /// <returns>当前位置为小写转大写或首字母缩写边界时返回 true</returns>
    private static bool StartsNewToken(string identifier, int index)
    {
        if (index == 0 || identifier[index - 1] == '_')
        {
            return false;
        }

        var previous = identifier[index - 1];
        var current = identifier[index];

        return (char.IsLower(previous) && char.IsUpper(current)) ||
            (char.IsUpper(previous) &&
             char.IsUpper(current) &&
             index + 1 < identifier.Length &&
             char.IsLower(identifier[index + 1]));
    }

    /// <summary>
    /// 判断一个已切分的标识符分段是否等于受治理品牌名称
    /// </summary>
    /// <param name="identifier">包含待匹配分段的完整声明标识符</param>
    /// <param name="startIndex">待匹配分段在完整标识符中的起始索引</param>
    /// <param name="length">待匹配分段的字符长度</param>
    /// <param name="forbiddenSegment">命中时返回标准化的受治理品牌名称</param>
    /// <returns>待匹配分段与任一受治理品牌名称忽略大小写相等时返回 true</returns>
    private static bool TryMatchForbiddenBrandSegment(
        string identifier,
        int startIndex,
        int length,
        out string forbiddenSegment)
    {
        foreach (var candidate in ForbiddenBrandSegments)
        {
            if (candidate.Length == length &&
                string.Compare(identifier, startIndex, candidate, 0, length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                forbiddenSegment = candidate;
                return true;
            }
        }

        forbiddenSegment = string.Empty;
        return false;
    }
}
