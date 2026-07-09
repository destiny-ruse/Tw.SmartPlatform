using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Tw.Analyzers.Rules;
using Xunit;

namespace Tw.Analyzers.Tests;

public sealed class ForbiddenIdentifierPrefixAnalyzerTests
{
    [Fact]
    public async Task ReportsTwPrefixExceptTwException()
    {
        const string source = """
        namespace Demo;
        public sealed class TwOrderService { }
        public sealed class TwException : System.Exception { }
        """;

        var tree = CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create(
            "Demo",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ForbiddenIdentifierPrefixAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        diagnostics.Should().Contain(diagnostic => diagnostic.Id == "TWGOV001");
        diagnostics.Should().ContainSingle();
    }
}
