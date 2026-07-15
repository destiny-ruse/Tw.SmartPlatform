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
    /// 返回拓扑清单中的每个淘汰包及两种受治理引用类型
    /// </summary>
    /// <returns>淘汰包标识与 MSBuild 引用类型组合</returns>
    public static IEnumerable<object[]> RetiredReferenceCases()
    {
        foreach (var retiredPackage in LoadCatalog().RetiredPackages)
        {
            yield return [retiredPackage.PackageId, "PackageReference"];
            yield return [retiredPackage.PackageId, "ProjectReference"];
        }
    }

    /// <summary>
    /// 返回具有替代目标的淘汰映射
    /// </summary>
    /// <returns>淘汰包与允许替代包的组合</returns>
    public static IEnumerable<object[]> AllowedReplacementCases()
    {
        return LoadCatalog().RetiredPackages
            .Where(package => package.ReplacementPackageId is not null)
            .Select(package => new object[] { package.PackageId, package.ReplacementPackageId! });
    }

    /// <summary>
    /// 验证拓扑清单中的每个淘汰包会同时阻止项目引用和包引用
    /// </summary>
    /// <param name="packageId">拓扑清单中的淘汰包标识</param>
    /// <param name="referenceType">需要验证的 MSBuild 引用类型</param>
    [Theory]
    [MemberData(nameof(RetiredReferenceCases))]
    public void Scan_FailsForEveryRetiredPackageReference(string packageId, string referenceType)
    {
        var include = referenceType == "ProjectReference"
            ? $"..\\{packageId}\\{packageId}.csproj"
            : packageId;
        var projectXml = $"<Project><ItemGroup><{referenceType} Include=\"{include}\" /></ItemGroup></Project>";

        var result = new ProjectDependencyScanner().ScanProjectText(
            "src/Billing/Billing.csproj",
            projectXml,
            LoadCatalog());

        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV002")
            .Which.Message.Should().Contain(packageId);
    }

    /// <summary>
    /// 验证淘汰包匹配不区分大小写
    /// </summary>
    /// <param name="referenceType">需要验证大小写匹配的 MSBuild 引用类型</param>
    [Theory]
    [InlineData("PackageReference")]
    [InlineData("ProjectReference")]
    public void Scan_MatchesRetiredPackagesCaseInsensitively(string referenceType)
    {
        var include = referenceType == "ProjectReference"
            ? "..\\tW.hTtP.cLiEnT\\tW.hTtP.cLiEnT.csproj"
            : "tW.hTtP.cLiEnT";
        var result = new ProjectDependencyScanner().ScanProjectText(
            "src/Billing/Billing.csproj",
            $"<Project><ItemGroup><{referenceType} Include=\"{include}\" /></ItemGroup></Project>",
            LoadCatalog());

        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV002");
    }

    /// <summary>
    /// 验证替代包不会被淘汰边界误判
    /// </summary>
    /// <param name="retiredPackageId">提供替代映射的淘汰包标识</param>
    /// <param name="replacementPackageId">允许引用的目标包标识</param>
    [Theory]
    [MemberData(nameof(AllowedReplacementCases))]
    public void Scan_AllowsReplacementPackages(string retiredPackageId, string replacementPackageId)
    {
        var result = new ProjectDependencyScanner().ScanProjectText(
            "src/Billing/Billing.csproj",
            $"<Project><ItemGroup><PackageReference Include=\"{replacementPackageId}\" /></ItemGroup></Project>",
            LoadCatalog());

        result.Errors.Should().NotContain(error =>
            error.Code == "TWGOV002" && error.Message.Contains(retiredPackageId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 验证 Tw.AspNetCore 不得引用运行时基础设施提供程序
    /// </summary>
    /// <param name="providerPackage">不得进入 Web 基础包的提供程序包</param>
    [Theory]
    [InlineData("Autofac")]
    [InlineData("Castle.Core")]
    [InlineData("Tw.Data.SqlSugar")]
    [InlineData("DotNetCore.CAP")]
    [InlineData("Quartz")]
    [InlineData("Yarp.ReverseProxy")]
    [InlineData("StackExchange.Redis")]
    public void Scan_FailsWhenTwAspNetCoreReferencesInfrastructureProvider(string providerPackage)
    {
        var result = ScanPackageReference(
            "backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Tw.AspNetCore.csproj",
            providerPackage);

        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV004");
    }

    /// <summary>
    /// 验证应用层和领域层不得引用基础设施提供程序
    /// </summary>
    /// <param name="projectPath">待检查的应用层或领域层项目路径</param>
    /// <param name="providerPackage">不得进入该层的提供程序包</param>
    [Theory]
    [InlineData("src/Billing.Application/Billing.Application.csproj", "SqlSugarCore")]
    [InlineData("src/Billing.Application/Billing.Application.csproj", "DotNetCore.CAP")]
    [InlineData("src/Billing.Application/Billing.Application.csproj", "Quartz")]
    [InlineData("src/Billing.Domain/Billing.Domain.csproj", "Yarp.ReverseProxy")]
    [InlineData("src/Billing.Domain/Billing.Domain.csproj", "StackExchange.Redis")]
    [InlineData("src/Billing.Domain/Billing.Domain.csproj", "Autofac")]
    [InlineData("src/Billing.Domain/Billing.Domain.csproj", "Castle.Core")]
    public void Scan_FailsWhenApplicationOrDomainReferencesInfrastructureProvider(
        string projectPath,
        string providerPackage)
    {
        var result = ScanPackageReference(projectPath, providerPackage);

        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV005");
    }

    /// <summary>
    /// 验证无效项目 XML 会返回稳定治理错误
    /// </summary>
    [Fact]
    public void Scan_FailsForMalformedProjectXml()
    {
        var result = new ProjectDependencyScanner().ScanProjectText(
            "src/Billing/Billing.csproj",
            "<Project><ItemGroup>",
            LoadCatalog());

        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV000");
    }

    /// <summary>
    /// 验证仓库目录缺失时扫描以稳定输入错误结束
    /// </summary>
    [Fact]
    public void ScanRepository_FailsWhenRepositoryDoesNotExist()
    {
        var missingRepository = Path.Combine(Path.GetTempPath(), $"tw-cli-missing-{Guid.NewGuid():N}");

        var result = new ProjectDependencyScanner().ScanRepository(missingRepository);

        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV000");
    }

    /// <summary>
    /// 验证生产项目引用测试基类时依赖扫描会失败
    /// </summary>
    [Fact]
    public void Scan_FailsWhenProductionProjectReferencesTestBase()
    {
        var scanner = new ProjectDependencyScanner();
        var result = scanner.ScanProjectText(
            projectPath: "src/Billing.Host/Billing.Host.csproj",
            projectXml: "<Project><ItemGroup><ProjectReference Include=\"..\\Tw.TestBase\\Tw.TestBase.csproj\" /></ItemGroup></Project>",
            packageCatalog: LoadCatalog());

        result.Errors.Should().Contain(error => error.Code == "TWGOV003");
    }

    /// <summary>
    /// 验证 BuildingBlocks 的 TestBase 夹具包可以组合其他测试基础包
    /// </summary>
    [Fact]
    public void Scan_AllowsTestBasePackagesToReferenceTestBaseDependencies()
    {
        var result = new ProjectDependencyScanner().ScanProjectText(
            "backend/dotnet/BuildingBlocks/src/TestBase/Tw.AspNetCore.TestBase/Tw.AspNetCore.TestBase.csproj",
            "<Project><ItemGroup><ProjectReference Include=\"..\\Tw.TestBase\\Tw.TestBase.csproj\" /></ItemGroup></Project>",
            LoadCatalog());

        result.Errors.Should().NotContain(error => error.Code == "TWGOV003");
    }

    /// <summary>
    /// 扫描单个 PackageReference 并返回治理结果
    /// </summary>
    /// <param name="projectPath">用于判定架构层的项目路径</param>
    /// <param name="packageId">待扫描的包标识</param>
    /// <returns>依赖治理扫描结果</returns>
    private static DependencyScanResult ScanPackageReference(string projectPath, string packageId)
    {
        return new ProjectDependencyScanner().ScanProjectText(
            projectPath,
            $"<Project><ItemGroup><PackageReference Include=\"{packageId}\" /></ItemGroup></Project>",
            LoadCatalog());
    }

    /// <summary>
    /// 从当前仓库的唯一拓扑清单加载淘汰包目录
    /// </summary>
    /// <returns>由 building-blocks-topology.json 构造的包目录</returns>
    private static ForbiddenPackageCatalog LoadCatalog()
    {
        return ForbiddenPackageCatalog.Load(FindRepositoryRoot());
    }

    /// <summary>
    /// 从测试输出目录向上定位当前仓库根目录
    /// </summary>
    /// <returns>包含 BuildingBlocks 拓扑清单的仓库根目录</returns>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var topologyPath = Path.Combine(
                directory.FullName,
                "backend",
                "dotnet",
                "BuildingBlocks",
                "building-blocks-topology.json");
            if (File.Exists(topologyPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("无法定位测试仓库根目录");
    }
}
