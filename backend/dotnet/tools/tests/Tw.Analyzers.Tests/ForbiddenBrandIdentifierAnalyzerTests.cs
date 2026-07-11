using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Tw.Analyzers.Rules;
using Xunit;

namespace Tw.Analyzers.Tests;

/// <summary>
/// 覆盖品牌标识符分段治理分析器的声明符号行为和例外边界
/// </summary>
public sealed class ForbiddenBrandIdentifierAnalyzerTests
{
    /// <summary>
    /// 验证品牌分段在各类声明符号及不同大小写和位置中均触发治理诊断
    /// </summary>
    /// <returns>表示异步验证流程完成状态的任务</returns>
    [Fact]
    public async Task ReportsForbiddenBrandSegmentsAcrossDeclarationKinds()
    {
        const string source = """
        public sealed class TwOrderService
        {
            public string twOrder { get; set; } = string.Empty;
            private string TW_ORDER = string.Empty;
            public event System.EventHandler? AddTwYarpGateway;

            public void OrderTwHandler(string AbpModule)
            {
                var FurionService = 0;
            }
        }
        """;

        var diagnostics = await GetDiagnosticsAsync(source, "BrandSegments");

        diagnostics.Should().OnlyContain(diagnostic => diagnostic.Id == "TWGOV001");
        diagnostics
            .Select(diagnostic => source.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length))
            .Should()
            .BeEquivalentTo(
            [
                "TwOrderService",
                "twOrder",
                "TW_ORDER",
                "AddTwYarpGateway",
                "OrderTwHandler",
                "AbpModule",
                "FurionService"
            ]);
    }

    /// <summary>
    /// 验证所有局部变量声明语法均纳入品牌分段治理
    /// </summary>
    /// <returns>表示异步验证流程完成状态的任务</returns>
    [Fact]
    public async Task ReportsForbiddenBrandSegmentsInAllLocalDeclarationForms()
    {
        const string source = """
        public sealed class LocalDeclarationHost
        {
            public void Process(System.Collections.Generic.IEnumerable<int> values, object value)
            {
                foreach (var TwItem in values)
                {
                }

                try
                {
                }
                catch (System.Exception AbpFailure)
                {
                }

                if (value is string FurionValue)
                {
                }

                var (TwLeft, TwRight) = (1, 2);
            }
        }
        """;

        var diagnostics = await GetDiagnosticsAsync(source, "LocalDeclarations");

        diagnostics.Should().OnlyContain(diagnostic => diagnostic.Id == "TWGOV001");
        diagnostics
            .Select(diagnostic => source.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length))
            .Should()
            .BeEquivalentTo(["TwItem", "AbpFailure", "FurionValue", "TwLeft", "TwRight"]);
    }

    /// <summary>
    /// 验证类型参数、using 别名、标签和查询范围变量均纳入品牌分段治理
    /// </summary>
    /// <returns>表示异步验证流程完成状态的任务</returns>
    [Fact]
    public async Task ReportsForbiddenBrandSegmentsInAdditionalDeclarationForms()
    {
        const string source = """
        using AbpAlias = System.String;
        using System.Linq;

        public sealed class GenericHost<TwTypeParameter>
        {
            public void Process(System.Collections.Generic.IEnumerable<int> values)
            {
            TwLabel:
                var query =
                    from FurionSource in values
                    let TwValue = FurionSource
                    join AbpMatch in values on FurionSource equals AbpMatch into TwMatches
                    select TwMatches into FurionContinuation
                    select FurionContinuation;

                if (query is null)
                {
                    goto TwLabel;
                }
            }
        }
        """;

        var diagnostics = await GetDiagnosticsAsync(source, "AdditionalDeclarations");

        diagnostics.Should().OnlyContain(diagnostic => diagnostic.Id == "TWGOV001");
        diagnostics
            .Select(diagnostic => source.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length))
            .Should()
            .BeEquivalentTo(
            [
                "AbpAlias",
                "TwTypeParameter",
                "TwLabel",
                "FurionSource",
                "TwValue",
                "AbpMatch",
                "TwMatches",
                "FurionContinuation"
            ]);
    }

    /// <summary>
    /// 验证仅 Tw.Core 程序集中继承 System.Exception 的 Tw.Exceptions.TwException 可免于诊断
    /// </summary>
    /// <returns>表示异步验证流程完成状态的任务</returns>
    [Fact]
    public async Task ReportsForbiddenBrandSegmentExceptApprovedException()
    {
        const string source = """
        namespace Tw.Exceptions
        {
            public sealed class TwException : System.Exception { }
        }

        namespace Other
        {
            public sealed class TwException : System.Exception { }

            public sealed class OtherType
            {
                public void TwException()
                {
                    var TwException = 0;
                }
            }
        }
        """;

        var diagnostics = await GetDiagnosticsAsync(source, "Tw.Core");

        diagnostics.Should().OnlyContain(diagnostic => diagnostic.Id == "TWGOV001");
        diagnostics
            .Select(diagnostic => source.Substring(
                diagnostic.Location.SourceSpan.Start,
                diagnostic.Location.SourceSpan.Length))
            .Should()
            .BeEquivalentTo(["TwException", "TwException", "TwException"]);
    }

    /// <summary>
    /// 验证泛型 TwException 不属于批准的异常类型
    /// </summary>
    /// <returns>表示异步验证流程完成状态的任务</returns>
    [Fact]
    public async Task ReportsGenericTwExceptionInTwCoreAssembly()
    {
        const string source = """
        namespace Tw.Exceptions
        {
            public sealed class TwException<T> : System.Exception { }
        }
        """;

        var diagnostics = await GetDiagnosticsAsync(source, "Tw.Core");

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "TWGOV001");
        source.Substring(
                diagnostics[0].Location.SourceSpan.Start,
                diagnostics[0].Location.SourceSpan.Length)
            .Should()
            .Be("TwException");
    }

    /// <summary>
    /// 验证未继承 System.Exception 的同命名类型不属于批准的异常类型
    /// </summary>
    /// <returns>表示异步验证流程完成状态的任务</returns>
    [Fact]
    public async Task ReportsNonExceptionTwExceptionInTwCoreAssembly()
    {
        const string source = """
        namespace Tw.Exceptions
        {
            public sealed class TwException { }
        }
        """;

        var diagnostics = await GetDiagnosticsAsync(source, "Tw.Core");

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "TWGOV001");
        source.Substring(
                diagnostics[0].Location.SourceSpan.Start,
                diagnostics[0].Location.SourceSpan.Length)
            .Should()
            .Be("TwException");
    }

    /// <summary>
    /// 验证相同全名的异常类型在非 Tw.Core 程序集中仍触发品牌分段治理诊断
    /// </summary>
    /// <returns>表示异步验证流程完成状态的任务</returns>
    [Fact]
    public async Task ReportsApprovedExceptionNameFromAnotherAssembly()
    {
        const string source = """
        namespace Tw.Exceptions
        {
            public sealed class TwException : System.Exception { }
        }
        """;

        var diagnostics = await GetDiagnosticsAsync(source, "Other.Core");

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "TWGOV001");
        source.Substring(
                diagnostics[0].Location.SourceSpan.Start,
                diagnostics[0].Location.SourceSpan.Length)
            .Should()
            .Be("TwException");
    }

    /// <summary>
    /// 验证嵌套在其他类型中的同名异常不属于批准的全限定类型
    /// </summary>
    /// <returns>表示异步验证流程完成状态的任务</returns>
    [Fact]
    public async Task ReportsNestedTwExceptionInTwCoreAssembly()
    {
        const string source = """
        namespace Tw.Exceptions
        {
            public sealed class Outer
            {
                public sealed class TwException : System.Exception { }
            }
        }
        """;

        var diagnostics = await GetDiagnosticsAsync(source, "Tw.Core");

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "TWGOV001");
        source.Substring(
                diagnostics[0].Location.SourceSpan.Start,
                diagnostics[0].Location.SourceSpan.Length)
            .Should()
            .Be("TwException");
    }

    /// <summary>
    /// 验证包含相同字母但不构成品牌分段的普通单词不会触发诊断
    /// </summary>
    /// <returns>表示异步验证流程完成状态的任务</returns>
    [Fact]
    public async Task DoesNotReportOrdinaryWordsContainingBrandLetters()
    {
        const string source = """
        public sealed class Twin
        {
            public string Twice { get; set; } = string.Empty;
            private string Between = string.Empty;
            public event System.EventHandler? Write;
        }
        """;

        var diagnostics = await GetDiagnosticsAsync(source, "OrdinaryWords");

        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// 验证命名空间、类型引用、字符串和注释中的品牌文本不会触发诊断
    /// </summary>
    /// <returns>表示异步验证流程完成状态的任务</returns>
    [Fact]
    public async Task DoesNotReportBrandTextOutsideDeclarations()
    {
        const string source = """
        using Tw.External;

        namespace Tw.External
        {
            public sealed class ReferenceType { }
        }

        namespace Neutral
        {
            public sealed class Host
            {
                private Tw.External.ReferenceType reference = new();

                public void Process()
                {
                    var message = "Tw Abp Furion";
                    // Tw Abp Furion
                    _ = typeof(Tw.External.ReferenceType);
                }
            }
        }
        """;

        var diagnostics = await GetDiagnosticsAsync(source, "NonDeclarations");

        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// 使用指定程序集名称编译源码并返回当前治理分析器报告的诊断
    /// </summary>
    /// <param name="source">用于构造受测编译单元的 C# 源码</param>
    /// <param name="assemblyName">受测编译单元的显式程序集名称</param>
    /// <returns>治理分析器为受测编译单元生成的诊断集合</returns>
    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, string assemblyName)
    {
        var tree = CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [tree],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ForbiddenBrandIdentifierAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }
}
