using AwesomeAssertions;
using Tw.Cli.Governance;
using Xunit;

namespace Tw.Cli.Tests;

/// <summary>验证 AuditDependenciesCommandTests 相关行为</summary>
public sealed class AuditDependenciesCommandTests
{
    /// <summary>验证 Scan_FailsWhenProductionProjectReferencesTestBase 场景</summary>
    [Fact]
    public void Scan_FailsWhenProductionProjectReferencesTestBase()
    {
        var scanner = new ProjectDependencyScanner();
        var result = scanner.ScanProjectText(
            projectPath: "src/Billing.Host/Billing.Host.csproj",
            projectXml: "<Project><ItemGroup><ProjectReference Include=\"..\\Tw.TestBase\\Tw.TestBase.csproj\" /></ItemGroup></Project>");

        result.Errors.Should().Contain(error => error.Code == "TWGOV003");
    }
}
