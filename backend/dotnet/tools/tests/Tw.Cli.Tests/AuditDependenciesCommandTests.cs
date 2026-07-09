using AwesomeAssertions;
using Tw.Cli.Governance;
using Xunit;

namespace Tw.Cli.Tests;

public sealed class AuditDependenciesCommandTests
{
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
