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
    /// 返回仓库中央版本表中的锁实现与 YARP 服务发现提供程序在受治理层和引用类型下的组合
    /// </summary>
    /// <returns>项目路径、提供程序包、引用类型和预期治理错误码</returns>
    public static IEnumerable<object[]> CanonicalInfrastructureProviderCases()
    {
        var providerPackages = new[]
        {
            "DistributedLock.Redis",
            "Microsoft.Extensions.ServiceDiscovery.Yarp"
        };
        var governedProjects = new[]
        {
            (Path: "backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Tw.AspNetCore.csproj", ErrorCode: "TWGOV004"),
            (Path: "src/Billing.Application/Billing.Application.csproj", ErrorCode: "TWGOV005"),
            (Path: "src/Billing.Domain/Billing.Domain.csproj", ErrorCode: "TWGOV005")
        };
        var referenceTypes = new[] { "PackageReference", "ProjectReference" };

        foreach (var providerPackage in providerPackages)
        {
            foreach (var governedProject in governedProjects)
            {
                foreach (var referenceType in referenceTypes)
                {
                    var referencePackage = providerPackage == "DistributedLock.Redis"
                        && governedProject.ErrorCode == "TWGOV004"
                        && referenceType == "ProjectReference"
                            ? providerPackage.ToUpperInvariant()
                            : providerPackage;
                    yield return [governedProject.Path, referencePackage, referenceType, governedProject.ErrorCode];
                }
            }
        }
    }

    /// <summary>
    /// 拓扑清单中必须统一映射为配置错误的非法 retiredPackages 结构
    /// </summary>
    public static TheoryData<string> InvalidCatalogJsonCases => new()
    {
        { "{\"retiredPackages\":null}" },
        { "{\"retiredPackages\":[null]}" },
        { "{\"retiredPackages\":{}}" },
        { "{\"retiredPackages\":[{\"packageId\":\"   \"}]}" },
        { "{\"retiredPackages\":[{\"packageId\":\"Tw.Legacy\"},{\"packageId\":\"tw.legacy\"}]}" }
    };

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
    public void Scan_FailsWhenWebHostPackageReferencesInfrastructureProvider(string providerPackage)
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
    /// 验证中央版本表中的锁实现和 YARP 服务发现提供程序在所有受治理层与引用类型中均被拒绝
    /// </summary>
    /// <param name="projectPath">待检查的 Tw.AspNetCore、Application 或 Domain 项目路径</param>
    /// <param name="providerPackage">中央版本表中的 canonical 提供程序包标识</param>
    /// <param name="referenceType">PackageReference 或 ProjectReference</param>
    /// <param name="expectedErrorCode">项目层对应的稳定治理错误码</param>
    [Theory]
    [MemberData(nameof(CanonicalInfrastructureProviderCases))]
    public void Scan_RejectsCanonicalInfrastructureProviderReferences(
        string projectPath,
        string providerPackage,
        string referenceType,
        string expectedErrorCode)
    {
        var include = referenceType == "ProjectReference"
            ? $"..\\{providerPackage}\\{providerPackage}.csproj"
            : providerPackage;
        var result = new ProjectDependencyScanner().ScanProjectText(
            projectPath,
            $"<Project><ItemGroup><{referenceType} Include=\"{include}\" /></ItemGroup></Project>",
            LoadCatalog());

        result.Errors.Should().ContainSingle(error => error.Code == expectedErrorCode);
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
    /// Include、Update 与分号 item-spec 都必须逐项执行淘汰包治理
    /// </summary>
    /// <param name="referenceType">PackageReference 或 ProjectReference</param>
    /// <param name="itemOperation">Include 或 Update</param>
    [Theory]
    [InlineData("PackageReference", "Include")]
    [InlineData("PackageReference", "Update")]
    [InlineData("ProjectReference", "Include")]
    [InlineData("ProjectReference", "Update")]
    public void Scan_GovernsSemicolonSeparatedIncludeAndUpdateItems(
        string referenceType,
        string itemOperation)
    {
        var governedItem = referenceType == "ProjectReference"
            ? "..\\Tw.Http.Client\\Tw.Http.Client.csproj"
            : "Tw.Http.Client";
        var allowedItem = referenceType == "ProjectReference"
            ? "..\\Tw.Http\\Tw.Http.csproj"
            : "Tw.Http";
        var result = new ProjectDependencyScanner().ScanProjectText(
            "src/Billing.Application/Billing.Application.csproj",
            $"<Project><ItemGroup><{referenceType} {itemOperation}=\"{governedItem}; {allowedItem}\" /></ItemGroup></Project>",
            LoadCatalog());

        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV002");
    }

    /// <summary>
    /// MSBuild item 类型大小写不能绕过依赖治理
    /// </summary>
    /// <param name="referenceType">混合大小写的 PackageReference 或 ProjectReference</param>
    /// <param name="itemSpec">对应淘汰包的 item-spec</param>
    [Theory]
    [InlineData("pAcKaGeReFeReNcE", "Tw.Http.Client")]
    [InlineData("pRoJeCtReFeReNcE", "..\\Tw.Http.Client\\Tw.Http.Client.csproj")]
    public void Scan_GovernsMsBuildItemNamesCaseInsensitively(string referenceType, string itemSpec)
    {
        var result = new ProjectDependencyScanner().ScanProjectText(
            "src/Billing.Application/Billing.Application.csproj",
            $"<Project><ItemGroup><{referenceType} Include=\"{itemSpec}\" /></ItemGroup></Project>",
            LoadCatalog());

        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV002");
    }

    /// <summary>
    /// Update 中的基础设施包标识不能绕过应用层边界
    /// </summary>
    [Fact]
    public void Scan_GovernsInfrastructureProviderUpdateItems()
    {
        var result = new ProjectDependencyScanner().ScanProjectText(
            "src/Billing.Application/Billing.Application.csproj",
            "<Project><ItemGroup><PackageReference Update=\"DistributedLock.Redis\" /></ItemGroup></Project>",
            LoadCatalog());

        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV005");
    }

    /// <summary>
    /// 无法静态求值的引用身份必须返回稳定配置错误而不是静默跳过
    /// </summary>
    /// <param name="referenceType">PackageReference 或 ProjectReference</param>
    /// <param name="itemOperation">Include 或 Update</param>
    [Theory]
    [InlineData("PackageReference", "Include")]
    [InlineData("PackageReference", "Update")]
    [InlineData("ProjectReference", "Include")]
    [InlineData("ProjectReference", "Update")]
    public void Scan_ReportsDynamicItemExpressions(string referenceType, string itemOperation)
    {
        var result = new ProjectDependencyScanner().ScanProjectText(
            "src/Billing.Application/Billing.Application.csproj",
            $"<Project><ItemGroup><{referenceType} {itemOperation}=\"$(GovernedDependency)\" /></ItemGroup></Project>",
            LoadCatalog());

        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV000");
    }

    /// <summary>
    /// 自动导入文件中的 MSBuildThisFileDirectory 是可静态求值的内建属性，不应产生配置误报
    /// </summary>
    [Fact]
    public void ScanRepository_AllowsMsBuildThisFileDirectoryProjectReference()
    {
        using var repository = TemporaryAuditRepository.Create();
        repository.WriteFile(
            "Directory.Build.targets",
            "<Project><ItemGroup><ProjectReference Include=\"$(MSBuildThisFileDirectory)tools/src/Tw.Analyzers/Tw.Analyzers.csproj\" /></ItemGroup></Project>");
        repository.WriteFile(
            "src/Billing.Application/Billing.Application.csproj",
            "<Project />");

        var result = new ProjectDependencyScanner().ScanRepository(repository.RootPath);

        result.Errors.Should().NotContain(error => error.Code == "TWGOV000");
    }

    /// <summary>
    /// 展开 MSBuildThisFileDirectory 后仍必须对项目文件名执行淘汰包治理
    /// </summary>
    [Fact]
    public void ScanRepository_GovernsRetiredProjectReferenceUsingMsBuildThisFileDirectory()
    {
        using var repository = TemporaryAuditRepository.Create();
        repository.WriteFile(
            "Directory.Build.targets",
            "<Project><ItemGroup><ProjectReference Include=\"$(MSBuildThisFileDirectory)src/Tw.Http.Client/Tw.Http.Client.csproj\" /></ItemGroup></Project>");
        repository.WriteFile(
            "src/Billing.Application/Billing.Application.csproj",
            "<Project />");

        var result = new ProjectDependencyScanner().ScanRepository(repository.RootPath);

        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV002");
        result.Errors.Should().NotContain(error => error.Code == "TWGOV000");
    }

    /// <summary>
    /// 仓库扫描必须递归读取根与祖先自动导入以及显式导入，并安全终止导入循环
    /// </summary>
    [Fact]
    public void ScanRepository_GovernsAutomaticAndExplicitImportsConservatively()
    {
        using var repository = TemporaryAuditRepository.Create();
        repository.WriteFile(
            "Directory.Build.props",
            "<Project><ItemGroup Condition=\"'$(Configuration)' == 'Never'\"><PackageReference Include=\"Tw.Http.Client\" /></ItemGroup></Project>");
        repository.WriteFile(
            "src/Directory.Build.targets",
            "<Project><ItemGroup><PackageReference Update=\"DistributedLock.Redis\" /></ItemGroup></Project>");
        repository.WriteFile(
            "rules/one.props",
            "<Project><Import Project=\"two.targets\" /><ItemGroup><ProjectReference Include=\"..\\Tw.Http.Client\\Tw.Http.Client.csproj\" /></ItemGroup></Project>");
        repository.WriteFile(
            "rules/two.targets",
            "<Project><Import Project=\"one.props\" /><ItemGroup><PackageReference Include=\"Microsoft.Extensions.ServiceDiscovery.Yarp\" /></ItemGroup></Project>");
        repository.WriteFile(
            "src/Billing.Application/Billing.Application.csproj",
            "<Project><Import Project=\"..\\..\\rules\\one.props\" /></Project>");

        var result = new ProjectDependencyScanner().ScanRepository(repository.RootPath);

        result.Errors.Should().Contain(error => error.Code == "TWGOV002");
        result.Errors.Should().Contain(error => error.Code == "TWGOV005");
        result.Errors.Should().NotContain(error => error.Code == "TWGOV000");
    }

    /// <summary>
    /// 已访问导入路径必须按目标宿主的文件系统大小写语义去重
    /// </summary>
    /// <param name="isWindows">是否模拟 Windows 宿主</param>
    /// <param name="expectedCount">两个仅大小写不同路径的预期集合大小</param>
    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    public void MsBuildPath_SelectsVisitedFileComparerForTargetHost(bool isWindows, int expectedCount)
    {
        var pathType = typeof(ProjectDependencyScanner).Assembly.GetType("Tw.Cli.Governance.MsBuildPath");
        var comparerMethod = pathType?.GetMethod(
            "FileSystemPathComparer",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        comparerMethod.Should().NotBeNull();
        var comparer = (StringComparer)comparerMethod!.Invoke(null, [isWindows])!;
        var visitedFiles = new HashSet<string>(comparer)
        {
            "rules/A.props",
            "rules/a.props"
        };

        visitedFiles.Should().HaveCount(expectedCount);
    }

    /// <summary>
    /// 大小写敏感文件系统中的两个同名异大小写导入必须分别扫描
    /// </summary>
    [Fact]
    public void ScanRepository_ScansCaseDistinctImportsOnCaseSensitiveHosts()
    {
        using var repository = TemporaryAuditRepository.Create();
        repository.WriteFile("rules/A.props", "<Project><Import Project=\"a.props\" /></Project>");
        repository.WriteFile(
            "rules/a.props",
            "<Project><ItemGroup><PackageReference Include=\"Tw.Http.Client\" /></ItemGroup></Project>");
        if (Directory.GetFiles(Path.Combine(repository.RootPath, "rules"), "*.props").Length != 2)
        {
            return;
        }

        repository.WriteFile(
            "src/Billing.Application/Billing.Application.csproj",
            "<Project><Import Project=\"..\\..\\rules\\A.props\" /></Project>");

        var result = new ProjectDependencyScanner().ScanRepository(repository.RootPath);

        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV002");
        result.Errors.Should().NotContain(error => error.Code == "TWGOV000");
    }

    /// <summary>
    /// Windows 中同一文件的大小写变体导入循环必须由已访问集合终止
    /// </summary>
    [Fact]
    public void ScanRepository_TerminatesCaseVariantImportLoopOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var repository = TemporaryAuditRepository.Create();
        repository.WriteFile("rules/A.props", "<Project><Import Project=\"a.props\" /></Project>");
        repository.WriteFile(
            "src/Billing.Application/Billing.Application.csproj",
            "<Project><Import Project=\"..\\..\\rules\\A.props\" /></Project>");

        var result = new ProjectDependencyScanner().ScanRepository(repository.RootPath);

        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// 物理路径检查器拒绝导入时必须 fail closed，且不得解析被拒绝文件内容
    /// </summary>
    [Fact]
    public void ScanRepository_RejectsImportWhenPhysicalInspectorReportsBoundaryEscape()
    {
        using var repository = TemporaryAuditRepository.Create();
        var deniedImportPath = repository.WriteFile(
            "rules/linked.props",
            "<Project><ItemGroup><PackageReference Include=\"Tw.Http.Client\" /></ItemGroup></Project>");
        repository.WriteFile(
            "src/Billing.Application/Billing.Application.csproj",
            "<Project><Import Project=\"..\\..\\rules\\linked.props\" /></Project>");
        var inspector = new DenyingPhysicalPathInspector(deniedImportPath);
        var scanner = new ProjectDependencyScanner(
            MsBuildPath.FileSystemPathComparer(OperatingSystem.IsWindows()),
            inspector);

        var result = scanner.ScanRepository(repository.RootPath);

        inspector.InspectedPaths.Should().Contain(path => path == Path.GetFullPath(deniedImportPath));
        result.Errors.Should().ContainSingle(error => error.Code == "TWGOV000");
        result.Errors.Should().NotContain(error => error.Code == "TWGOV002");
    }

    /// <summary>
    /// 仓库内目录链接指向仓库外导入时必须拒绝，且不得读取外部项目内容
    /// </summary>
    [Fact]
    public void ScanRepository_RejectsImportThroughExternalDirectoryLink()
    {
        using var repository = TemporaryAuditRepository.Create();
        var externalRoot = Path.Combine(Path.GetTempPath(), $"tw-audit-external-{Guid.NewGuid():N}");
        var linkPath = Path.Combine(repository.RootPath, "rules", "external");
        var linkCreated = false;
        try
        {
            Directory.CreateDirectory(externalRoot);
            File.WriteAllText(
                Path.Combine(externalRoot, "retired.props"),
                "<Project><ItemGroup><PackageReference Include=\"Tw.Http.Client\" /></ItemGroup></Project>");
            Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);
            try
            {
                Directory.CreateSymbolicLink(linkPath, externalRoot);
                linkCreated = true;
            }
            catch (Exception exception) when (OperatingSystem.IsWindows()
                && exception is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
            {
                return;
            }

            repository.WriteFile(
                "src/Billing.Application/Billing.Application.csproj",
                "<Project><Import Project=\"..\\..\\rules\\external\\retired.props\" /></Project>");

            var result = new ProjectDependencyScanner().ScanRepository(repository.RootPath);

            result.Errors.Should().ContainSingle(error => error.Code == "TWGOV000");
            result.Errors.Should().NotContain(error => error.Code == "TWGOV002");
        }
        finally
        {
            if (linkCreated)
            {
                Directory.Delete(linkPath);
            }

            if (Directory.Exists(externalRoot))
            {
                Directory.Delete(externalRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 坏 XML 与动态显式导入必须统一返回 TWGOV000
    /// </summary>
    [Fact]
    public void ScanRepository_ReportsInvalidAndDynamicImports()
    {
        using var repository = TemporaryAuditRepository.Create();
        repository.WriteFile("rules/broken.props", "<Project><ItemGroup>");
        repository.WriteFile(
            "src/Billing.Application/Billing.Application.csproj",
            "<Project><Import Project=\"..\\..\\rules\\broken.props\" /><Import Project=\"$(RulePath)\" /></Project>");

        var result = new ProjectDependencyScanner().ScanRepository(repository.RootPath);

        result.Errors.Should().HaveCount(2)
            .And.OnlyContain(error => error.Code == "TWGOV000");
    }

    /// <summary>
    /// 模板与构建输出中的项目样例不得进入仓库治理扫描
    /// </summary>
    [Fact]
    public void ScanRepository_SkipsTemplatesAndBuildOutputs()
    {
        using var repository = TemporaryAuditRepository.Create();
        var retiredProject = "<Project><ItemGroup><PackageReference Include=\"Tw.Http.Client\" /></ItemGroup></Project>";
        repository.WriteFile("templates/service/Template.csproj", retiredProject);
        repository.WriteFile("src/Billing.Application/bin/Debug/Generated.csproj", retiredProject);
        repository.WriteFile("src/Billing.Application/obj/Generated.csproj", retiredProject);
        repository.WriteFile("src/Billing.Application/Billing.Application.csproj", "<Project />");

        var result = new ProjectDependencyScanner().ScanRepository(repository.RootPath);

        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// 非法 catalog JSON shape 必须统一抛出 GovernanceConfigurationException
    /// </summary>
    /// <param name="topologyJson">包含非法 retiredPackages 结构的清单文本</param>
    [Theory]
    [MemberData(nameof(InvalidCatalogJsonCases))]
    public void ForbiddenPackageCatalog_RejectsInvalidJsonShapes(string topologyJson)
    {
        using var repository = TemporaryAuditRepository.Create(topologyJson);

        var load = () => ForbiddenPackageCatalog.Load(repository.RootPath);

        load.Should().Throw<GovernanceConfigurationException>();
    }

    /// <summary>
    /// 仓库扫描必须把所有非法 catalog shape 映射为稳定 TWGOV000
    /// </summary>
    /// <param name="topologyJson">包含非法 retiredPackages 结构的清单文本</param>
    [Theory]
    [MemberData(nameof(InvalidCatalogJsonCases))]
    public void ScanRepository_MapsInvalidCatalogShapesToGovernanceDiagnostic(string topologyJson)
    {
        using var repository = TemporaryAuditRepository.Create(topologyJson);

        var result = new ProjectDependencyScanner().ScanRepository(repository.RootPath);

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
    /// CLI 路径规范化必须同时处理 Windows 与 Unix 分隔符
    /// </summary>
    /// <param name="itemSpec">包含混合分隔符的项目引用路径</param>
    /// <param name="directorySeparator">模拟目标宿主的目录分隔符</param>
    /// <param name="expected">目标宿主应接收的路径文本</param>
    [Theory]
    [InlineData("..\\Tw.Http.Client/Tw.Http.Client.csproj", '/', "../Tw.Http.Client/Tw.Http.Client.csproj")]
    [InlineData("../Tw.Http.Client\\Tw.Http.Client.csproj", '\\', "..\\Tw.Http.Client\\Tw.Http.Client.csproj")]
    public void MsBuildPath_UsesTargetHostSeparator(
        string itemSpec,
        char directorySeparator,
        string expected)
    {
        var helperType = typeof(ProjectDependencyScanner).Assembly.GetType(
            "Tw.Cli.Governance.MsBuildPath",
            throwOnError: true)!;
        var method = helperType.GetMethod(
            "NormalizeFileSystemPath",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;

        method.Invoke(null, [itemSpec, directorySeparator]).Should().Be(expected);
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

    /// <summary>
    /// 提供可回收的最小依赖审计仓库
    /// </summary>
    private sealed class TemporaryAuditRepository : IDisposable
    {
        /// <summary>
        /// 初始化临时仓库路径
        /// </summary>
        /// <param name="rootPath">测试专用仓库根目录</param>
        private TemporaryAuditRepository(string rootPath)
        {
            RootPath = rootPath;
        }

        /// <summary>
        /// 临时仓库根目录
        /// </summary>
        internal string RootPath { get; }

        /// <summary>
        /// 创建包含最小有效拓扑清单的临时仓库
        /// </summary>
        /// <returns>需要在用例结束后释放的仓库</returns>
        internal static TemporaryAuditRepository Create(string? topologyJson = null)
        {
            var repository = new TemporaryAuditRepository(
                Path.Combine(Path.GetTempPath(), $"tw-audit-tests-{Guid.NewGuid():N}"));
            repository.WriteFile(
                "backend/dotnet/BuildingBlocks/building-blocks-topology.json",
                topologyJson
                ?? "{\"retiredPackages\":[{\"packageId\":\"Tw.Http.Client\",\"replacementPackageId\":\"Tw.Http\"}]}");
            return repository;
        }

        /// <summary>
        /// 在仓库相对路径写入测试文本
        /// </summary>
        /// <param name="relativePath">使用正斜杠的仓库相对路径</param>
        /// <param name="content">写入文件的完整文本</param>
        /// <returns>写入文件的绝对路径</returns>
        internal string WriteFile(string relativePath, string content)
        {
            var path = Path.Combine(
                RootPath,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            return path;
        }

        /// <summary>
        /// 删除测试创建的临时目录
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }

    /// <summary>
    /// 对指定路径返回越界结果的确定性物理路径检查器
    /// </summary>
    private sealed class DenyingPhysicalPathInspector : IRepositoryPhysicalPathInspector
    {
        /// <summary>
        /// 模拟物理越界的规范化绝对路径
        /// </summary>
        private readonly string _deniedPath;

        /// <summary>
        /// 当前宿主文件系统使用的路径比较器
        /// </summary>
        private readonly StringComparer _pathComparer = MsBuildPath.FileSystemPathComparer(OperatingSystem.IsWindows());

        /// <summary>
        /// 使用需要拒绝的绝对路径创建检查器
        /// </summary>
        /// <param name="deniedPath">需要模拟物理越界的已存在文件</param>
        internal DenyingPhysicalPathInspector(string deniedPath)
        {
            _deniedPath = Path.GetFullPath(deniedPath);
        }

        /// <summary>
        /// 记录扫描器实际请求检查的路径
        /// </summary>
        internal List<string> InspectedPaths { get; } = [];

        /// <inheritdoc />
        public bool IsWithinRepository(string existingPath, string repositoryRoot)
        {
            var fullPath = Path.GetFullPath(existingPath);
            InspectedPaths.Add(fullPath);
            return !_pathComparer.Equals(fullPath, _deniedPath);
        }
    }
}
