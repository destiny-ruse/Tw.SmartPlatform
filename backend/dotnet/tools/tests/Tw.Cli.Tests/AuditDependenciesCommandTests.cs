using AwesomeAssertions;
using Tw.Cli.Governance;
using Xunit;

namespace Tw.Cli.Tests;

/// <summary>
/// 覆盖审计Dependencies命令的核心行为和边界条件
/// </summary>
public sealed class AuditDependenciesCommandTests
{
    /// <summary>
    /// 验证生产项目引用测试基类时依赖扫描会失败
    /// </summary>
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
