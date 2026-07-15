using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using AwesomeAssertions;
using Xunit;

namespace Tw.Templates.Tests;

/// <summary>
/// 覆盖模板冒烟的核心行为和边界条件
/// </summary>
public sealed class TemplateSmokeTests
{
    /// <summary>
    /// 验证纯模板包不生成与 lib/ref 输出不匹配的依赖组
    /// </summary>
    [Fact]
    public void TemplatePackage_SuppressesDependencyGroupsWhenPacking()
    {
        var projectFile = Path.Combine(FindToolRoot(), "src", "Tw.Templates", "Tw.Templates.csproj");
        var document = XDocument.Load(projectFile);

        document.Descendants("PackageType").Single().Value.Should().Be("Template");
        document.Descendants("IncludeBuildOutput").Single().Value.Should().Be("false");
        document.Descendants("SuppressDependenciesWhenPacking").Single().Value.Should().Be("true");
    }

    /// <summary>
    /// 验证模板项目与锁文件不引用已退役或禁止的包
    /// </summary>
    [Fact]
    public void TemplateContent_DoesNotReferenceRetiredOrForbiddenPackages()
    {
        var repositoryRoot = FindRepositoryRoot();
        var templateRoot = Path.Combine(repositoryRoot, "backend", "dotnet", "tools", "src", "Tw.Templates", "content");
        var forbiddenPackageIds = LoadRetiredPackageIds(repositoryRoot);
        var referencedPackageIds = Directory
            .GetFiles(templateRoot, "*.csproj", SearchOption.AllDirectories)
            .SelectMany(ReadProjectPackageIdentities)
            .Concat(Directory
                .GetFiles(templateRoot, "packages.lock.json", SearchOption.AllDirectories)
                .SelectMany(ReadLockPackageIdentities))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        referencedPackageIds.Should().NotContain(packageId =>
            forbiddenPackageIds.Contains(packageId, StringComparer.OrdinalIgnoreCase)
            || IsPackageFamily(packageId, "Autofac")
            || IsPackageFamily(packageId, "Castle"));
    }

    /// <summary>
    /// 验证服务模板不引用禁止包
    /// </summary>
    [Fact]
    public void ServiceTemplate_DoesNotReferenceForbiddenPackages()
    {
        var root = Path.Combine(FindToolRoot(), "src", "Tw.Templates", "content", "service");
        var files = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories);
        var text = string.Join(Environment.NewLine, files.Select(File.ReadAllText));

        text.Should().NotContain("Tw.Infrastructure");
        text.Should().NotContain("Tw.UnitOfWork");
        text.Should().NotContain("Tw.Data.Abstractions");
        text.Should().NotContain("MassTransit");
    }

    /// <summary>
    /// 验证网关模板在源码仓库内使用项目引用兜底
    /// </summary>
    [Fact]
    public void GatewayTemplate_UsesRepositoryProjectReferencesForInternalPackages()
    {
        var projectFile = Path.Combine(
            FindToolRoot(),
            "src",
            "Tw.Templates",
            "content",
            "gateway",
            "src",
            "Company.Gateway.Host",
            "Company.Gateway.Host.csproj");

        var document = XDocument.Load(projectFile);
        var projectReferences = document
            .Descendants("ProjectReference")
            .Select(reference => NormalizeProjectPath(reference.Attribute("Include")?.Value))
            .ToArray();

        projectReferences.Should().Contain([
            "../../../../../../../BuildingBlocks/src/Gateway/Tw.Gateway/Tw.Gateway.csproj",
            "../../../../../../../BuildingBlocks/src/Gateway/Tw.Gateway.Yarp/Tw.Gateway.Yarp.csproj",
            "../../../../../../../BuildingBlocks/src/Web/Tw.AspNetCore/Tw.AspNetCore.csproj",
            "../../../../../../../BuildingBlocks/src/Observability/Tw.Observability/Tw.Observability.csproj",
            "../../../../../../../BuildingBlocks/src/Configuration/Tw.Configuration/Tw.Configuration.csproj"
        ]);

        var internalPackageReferences = document
            .Descendants("PackageReference")
            .Where(reference => reference.Attribute("Include")?.Value.StartsWith("Tw.", StringComparison.Ordinal) == true)
            .ToArray();

        internalPackageReferences.Should().OnlyContain(reference => UsesPackageFallbackCondition(reference));
    }

    /// <summary>
    /// 验证模板中的仓库条件项目引用均指向真实项目
    /// </summary>
    [Fact]
    public void RepositoryConditionalProjectReferences_ResolveToExistingProjects()
    {
        var templateRoot = Path.Combine(FindToolRoot(), "src", "Tw.Templates", "content");
        var conditionalReferences = Directory
            .GetFiles(templateRoot, "*.csproj", SearchOption.AllDirectories)
            .SelectMany(projectFile => XDocument
                .Load(projectFile)
                .Descendants("ProjectReference")
                .Where(IsRepositoryConditionalReference)
                .Select(reference => new
                {
                    ProjectFile = projectFile,
                    ReferencedProject = Path.GetFullPath(
                        reference.Attribute("Include")!.Value,
                        Path.GetDirectoryName(projectFile)!)
                }))
            .ToArray();

        conditionalReferences.Should().NotBeEmpty();
        conditionalReferences.Should().OnlyContain(reference => File.Exists(reference.ReferencedProject));
    }

    /// <summary>
    /// 验证构建块模板采用 capability 目录并让测试项目引用运行时项目
    /// </summary>
    [Fact]
    public void BuildingBlockTemplate_UsesGovernedCapabilityLayoutAndRuntimeReference()
    {
        var templateRoot = Path.Combine(FindToolRoot(), "src", "Tw.Templates", "content", "building-block");
        var runtimeProject = Path.Combine(templateRoot, "src", "Capability", "Tw.Sample", "Tw.Sample.csproj");
        var testProject = Path.Combine(
            templateRoot,
            "tests",
            "Capability",
            "Tw.Sample.Tests",
            "Tw.Sample.Tests.csproj");

        File.Exists(runtimeProject).Should().BeTrue();
        File.Exists(testProject).Should().BeTrue();

        var runtimeReference = XDocument
            .Load(testProject)
            .Descendants("ProjectReference")
            .Single();
        var referencedProject = Path.GetFullPath(
            runtimeReference.Attribute("Include")!.Value,
            Path.GetDirectoryName(testProject)!);

        referencedProject.Should().Be(Path.GetFullPath(runtimeProject));
    }

    /// <summary>
    /// 验证构建块模板要求治理参数并生成可执行的 charter 边界
    /// </summary>
    [Fact]
    public void BuildingBlockTemplate_RequiresGovernanceParametersAndMachineReadableCharter()
    {
        var templateRoot = Path.Combine(FindToolRoot(), "src", "Tw.Templates", "content", "building-block");
        var templateConfiguration = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            templateRoot,
            ".template.config",
            "template.json")));
        templateConfiguration.RootElement
            .TryGetProperty("symbols", out var symbols)
            .Should()
            .BeTrue("构建块模板必须声明治理参数");
        var requiredSymbols = new[]
        {
            "capability",
            "owner",
            "responsibility",
            "inScope",
            "outOfScope",
            "publicCapability"
        };

        foreach (var symbolName in requiredSymbols)
        {
            symbols.GetProperty(symbolName).GetProperty("isRequired").GetBoolean().Should().BeTrue();
        }

        foreach (var mapping in new[]
                 {
                     (Symbol: "ownerYaml", Source: "owner", Placeholder: "__OWNER__"),
                     (Symbol: "responsibilityYaml", Source: "responsibility", Placeholder: "__RESPONSIBILITY__"),
                     (Symbol: "inScopeYaml", Source: "inScope", Placeholder: "__IN_SCOPE__"),
                     (Symbol: "outOfScopeYaml", Source: "outOfScope", Placeholder: "__OUT_OF_SCOPE__"),
                     (Symbol: "publicCapabilityYaml", Source: "publicCapability", Placeholder: "__PUBLIC_CAPABILITY__")
                 })
        {
            var symbol = symbols.GetProperty(mapping.Symbol);
            symbol.GetProperty("type").GetString().Should().Be("derived");
            symbol.GetProperty("valueSource").GetString().Should().Be(mapping.Source);
            symbol.GetProperty("valueTransform").GetString().Should().Be("jsonEncode");
            symbol.GetProperty("replaces").GetString().Should().Be(mapping.Placeholder);
        }

        templateConfiguration.RootElement
            .GetProperty("forms")
            .GetProperty("jsonEncode")
            .GetProperty("identifier")
            .GetString()
            .Should()
            .Be("jsonEncode");

        symbols
            .GetProperty("capability")
            .GetProperty("fileRename")
            .GetString()
            .Should()
            .Be("Capability");
        symbols
            .GetProperty("capability")
            .GetProperty("replaces")
            .GetString()
            .Should()
            .Be("Capability");

        var charterPath = Path.Combine(
            templateRoot,
            "src",
            "Capability",
            "Tw.Sample",
            "package-charter.yaml");
        var charter = File.ReadAllText(charterPath);

        charter.Should().Contain("__OWNER__");
        charter.Should().Contain("__RESPONSIBILITY__");
        charter.Should().Contain("__IN_SCOPE__");
        charter.Should().Contain("__OUT_OF_SCOPE__");
        charter.Should().Contain("__PUBLIC_CAPABILITY__");
        charter.Should().Contain("- \"*TestBase\"");

        foreach (var retiredPackageId in LoadRetiredPackageIds(FindRepositoryRoot()))
        {
            charter.Should().Contain($"- {retiredPackageId}");
        }

        charter.Should().NotContain("占位");
        charter.Should().NotContain("生成后");
        charter.Should().NotContain("test-only packages");
        charter.Should().NotContain("retired framework package names");
    }

    /// <summary>
    /// 验证真实打包、安装与实例化会把 YAML 敏感治理参数编码为可逆的字符串值
    /// </summary>
    [Fact]
    public void BuildingBlockTemplate_RealPackagePreservesYamlSensitiveGovernanceValues()
    {
        var repositoryRoot = FindRepositoryRoot();
        var templateProject = Path.Combine(
            repositoryRoot,
            "backend",
            "dotnet",
            "tools",
            "src",
            "Tw.Templates",
            "Tw.Templates.csproj");
        var validatorScript = Path.Combine(FindToolRoot(), "scripts", "Test-TemplateInstantiation.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var feedRoot = Path.Combine(testRoot, "feed");
        var hiveRoot = Path.Combine(testRoot, "hive");
        var outputRoot = Path.Combine(testRoot, "generated");
        var parsedValuesPath = Path.Combine(testRoot, "parsed-values.json");
        const string version = "0.1.0-yaml-safety-test";
        const string owner = "平台团队: 'owner' # \"双引号\" \\路径\n第二行";
        const string responsibility = "提供 A: B # 'single' \"quoted\" \\路径\n第二行";
        const string inScope = "处理范围: A # '单引号' \"引号\" \\边界\n下一项";
        const string outOfScope = "不处理范围: B # '单引号' \"引号\" \\边界\n下一项";
        const string publicCapability = "Tw.YamlSafety";
        Directory.CreateDirectory(feedRoot);

        try
        {
            var pack = RunDotNet(
                testRoot,
                "pack",
                templateProject,
                "-c",
                "Release",
                "--no-restore",
                "--nologo",
                "-o",
                feedRoot,
                $"-p:PackageVersion={version}");
            pack.ExitCode.Should().Be(0, pack.CombinedOutput);
            var packagePath = Path.Combine(feedRoot, $"Tw.Templates.{version}.nupkg");
            File.Exists(packagePath).Should().BeTrue(pack.CombinedOutput);

            var install = RunDotNet(
                testRoot,
                "new",
                "--debug:custom-hive",
                hiveRoot,
                "install",
                packagePath);
            install.ExitCode.Should().Be(0, install.CombinedOutput);

            var instantiate = RunDotNet(
                testRoot,
                "new",
                "--debug:custom-hive",
                hiveRoot,
                "tw-building-block",
                "--name",
                "Tw.YamlSafety",
                "--output",
                outputRoot,
                "--capability",
                "YamlSafety",
                "--owner",
                owner,
                "--responsibility",
                responsibility,
                "--inScope",
                inScope,
                "--outOfScope",
                outOfScope,
                "--publicCapability",
                publicCapability);
            instantiate.ExitCode.Should().Be(0, instantiate.CombinedOutput);

            var charterPath = Path.Combine(
                outputRoot,
                "src",
                "YamlSafety",
                "Tw.YamlSafety",
                "package-charter.yaml");
            File.Exists(charterPath).Should().BeTrue(instantiate.CombinedOutput);
            var toolsSource = Path.Combine(repositoryRoot, "tools", "src");
            var harness = CreatePowerShellHarness(
                validatorScript,
                ["Resolve-PythonCommand", "Invoke-CharterValidator"],
                $$"""
                Invoke-CharterValidator -CharterPath {{PowerShellLiteral(charterPath)}} -RepositoryRoot {{PowerShellLiteral(repositoryRoot)}}
                $pythonCommand = Resolve-PythonCommand
                $probe = 'from pathlib import Path; import json, sys; tools_source = Path(sys.argv[1]).resolve(strict=True); sys.path.insert(0, str(tools_source)); from tw_memory.charter import load_charter; charter = load_charter(Path(sys.argv[2])); values = dict(owner=charter.owner, responsibility=charter.responsibility, inScope=charter.in_scope[0], outOfScope=charter.out_of_scope[0], publicCapability=charter.public_capabilities[0]); Path(sys.argv[3]).write_text(json.dumps(values))'
                & $pythonCommand -I -c $probe {{PowerShellLiteral(toolsSource)}} {{PowerShellLiteral(charterPath)}} {{PowerShellLiteral(parsedValuesPath)}}
                if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
                """);
            var validation = RunPowerShell(testRoot, harness);

            validation.ExitCode.Should().Be(0, validation.CombinedOutput);
            using var parsedValues = JsonDocument.Parse(File.ReadAllText(parsedValuesPath));
            parsedValues.RootElement.GetProperty("owner").GetString().Should().Be(owner);
            parsedValues.RootElement.GetProperty("responsibility").GetString().Should().Be(responsibility);
            parsedValues.RootElement.GetProperty("inScope").GetString().Should().Be(inScope);
            parsedValues.RootElement.GetProperty("outOfScope").GetString().Should().Be(outOfScope);
            parsedValues.RootElement.GetProperty("publicCapability").GetString().Should().Be(publicCapability);
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证仓库品牌分析器接线排除自身、测试项目与模板内容
    /// </summary>
    [Fact]
    public void RepositoryBrandAnalyzer_ExcludesAnalyzerProjectsAndTemplateContent()
    {
        var dotnetRoot = Path.Combine(FindRepositoryRoot(), "backend", "dotnet");
        var targetsPath = Path.Combine(dotnetRoot, "Directory.Build.targets");

        File.Exists(targetsPath).Should().BeTrue();

        var targets = File.ReadAllText(targetsPath);
        targets.Should().Contain("tools/src/Tw.Analyzers/Tw.Analyzers.csproj");
        targets.Should().Contain("Tw.Analyzers.Tests");
        targets.Should().Contain("tools/src/Tw.Templates/content");
        targets.Should().Contain("$(WarningsAsErrors);TWGOV001");
    }

    /// <summary>
    /// 验证同名但不在批准完整路径上的项目仍接入品牌 analyzer 并阻断违规声明
    /// </summary>
    /// <param name="projectName">伪装成批准项目的 MSBuildProjectName</param>
    [Theory]
    [InlineData("Tw.Analyzers")]
    [InlineData("Tw.Analyzers.Tests")]
    public void RepositoryBrandAnalyzer_SameNameOutsideApprovedPathRemainsEnforced(string projectName)
    {
        var dotnetRoot = Path.Combine(FindRepositoryRoot(), "backend", "dotnet");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var projectPath = Path.Combine(testRoot, $"{projectName}.csproj");
        Directory.CreateDirectory(testRoot);

        try
        {
            File.WriteAllText(
                projectPath,
                $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <Import Project="{{Path.Combine(dotnetRoot, "Directory.Build.props")}}" />
                  <Import Project="{{Path.Combine(dotnetRoot, "Directory.Build.targets")}}" />
                </Project>
                """);
            File.WriteAllText(Path.Combine(testRoot, "Forbidden.cs"), "public sealed class TwSameNameProbe { }");

            var wiring = EvaluateAnalyzerWiring(projectPath);
            wiring.Enabled.Should().BeTrue("只有批准的完整项目路径可以排除 analyzer");
            wiring.AnalyzerReferences.Should().ContainSingle();
            wiring.Twgov001IsError.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证仅批准的 analyzer 源项目与测试项目完整路径排除自动 analyzer 引用
    /// </summary>
    [Fact]
    public void RepositoryBrandAnalyzer_ApprovedExactProjectPathsAreExcluded()
    {
        var dotnetRoot = Path.Combine(FindRepositoryRoot(), "backend", "dotnet");
        var approvedProjects = new[]
        {
            Path.Combine(dotnetRoot, "tools", "src", "Tw.Analyzers", "Tw.Analyzers.csproj"),
            Path.Combine(dotnetRoot, "tools", "tests", "Tw.Analyzers.Tests", "Tw.Analyzers.Tests.csproj")
        };

        foreach (var projectPath in approvedProjects)
        {
            var wiring = EvaluateAnalyzerWiring(projectPath);
            wiring.Enabled.Should().BeFalse(projectPath);
            wiring.AnalyzerReferences.Should().BeEmpty(projectPath);
            wiring.Twgov001IsError.Should().BeFalse(projectPath);
        }
    }

    /// <summary>
    /// 验证批准项目路径仅在 Windows 比较模式下忽略大小写，Unix 模式保留大小写差异
    /// </summary>
    /// <param name="comparisonMode">待模拟的路径比较模式</param>
    /// <param name="expectedEnabled">是否应启用品牌 analyzer</param>
    [Theory]
    [InlineData("Windows", false)]
    [InlineData("Unix", true)]
    public void RepositoryBrandAnalyzer_ApprovedPathCaseSensitivityFollowsComparisonMode(
        string comparisonMode,
        bool expectedEnabled)
    {
        var dotnetRoot = Path.Combine(FindRepositoryRoot(), "backend", "dotnet");
        var analyzerProject = Path.Combine(
            dotnetRoot,
            "tools",
            "src",
            "Tw.Analyzers",
            "Tw.Analyzers.csproj");
        var caseDifferentApprovedPath = ToggleAsciiPathCase(analyzerProject);

        var wiring = EvaluateAnalyzerWiring(
            analyzerProject,
            new Dictionary<string, string>
            {
                ["_TwPathComparisonMode"] = comparisonMode,
                ["_TwAnalyzerProjectFullPathInput"] = caseDifferentApprovedPath
            });

        wiring.Enabled.Should().Be(expectedEnabled);
        wiring.AnalyzerReferences.Should().HaveCount(expectedEnabled ? 1 : 0);
        wiring.Twgov001IsError.Should().Be(expectedEnabled);
    }

    /// <summary>
    /// 验证模板 Content 根路径仅在 Windows 模式忽略大小写，Unix 模式不会排除大小写不同的根
    /// </summary>
    /// <param name="comparisonMode">待模拟的路径比较模式</param>
    /// <param name="expectedEnabled">是否应启用品牌 analyzer</param>
    [Theory]
    [InlineData("Windows", false)]
    [InlineData("Unix", true)]
    public void RepositoryBrandAnalyzer_TemplateRootCaseSensitivityFollowsComparisonMode(
        string comparisonMode,
        bool expectedEnabled)
    {
        var dotnetRoot = Path.Combine(FindRepositoryRoot(), "backend", "dotnet");
        var templateContentRoot = Path.Combine(dotnetRoot, "tools", "src", "Tw.Templates", "content");
        var templateProject = Path.Combine(
            templateContentRoot,
            "building-block",
            "src",
            "Capability",
            "Tw.Sample",
            "Tw.Sample.csproj");

        var wiring = EvaluateAnalyzerWiring(
            templateProject,
            new Dictionary<string, string>
            {
                ["_TwPathComparisonMode"] = comparisonMode,
                ["_TwTemplateContentRootInput"] = ToggleAsciiPathCase(templateContentRoot)
            });

        wiring.Enabled.Should().Be(expectedEnabled);
        wiring.AnalyzerReferences.Should().HaveCount(expectedEnabled ? 1 : 0);
        wiring.Twgov001IsError.Should().Be(expectedEnabled);
    }

    /// <summary>
    /// 验证模板内容仅按规范化目录边界排除，前缀相似的兄弟目录仍执行品牌治理
    /// </summary>
    [Fact]
    public void RepositoryBrandAnalyzer_TemplateContentExclusionUsesDirectoryBoundary()
    {
        var dotnetRoot = Path.Combine(FindRepositoryRoot(), "backend", "dotnet");
        var templateProject = Path.Combine(
            dotnetRoot,
            "tools",
            "src",
            "Tw.Templates",
            "content",
            "building-block",
            "src",
            "Capability",
            "Tw.Sample",
            "Tw.Sample.csproj");
        var templateWiring = EvaluateAnalyzerWiring(templateProject);
        templateWiring.Enabled.Should().BeFalse();
        templateWiring.AnalyzerReferences.Should().BeEmpty();
        templateWiring.Twgov001IsError.Should().BeFalse();

        var siblingParent = Path.Combine(
            dotnetRoot,
            "tools",
            "src",
            "Tw.Templates",
            "content-sibling-probe");
        var siblingRoot = Path.Combine(siblingParent, Guid.NewGuid().ToString("N"));
        var siblingProject = Path.Combine(siblingRoot, "TemplateSibling.csproj");
        Directory.CreateDirectory(siblingRoot);
        try
        {
            File.WriteAllText(
                siblingProject,
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
            File.WriteAllText(Path.Combine(siblingRoot, "Forbidden.cs"), "public sealed class TwTemplateSiblingProbe { }");

            var siblingWiring = EvaluateAnalyzerWiring(siblingProject);
            siblingWiring.Enabled.Should().BeTrue();
            siblingWiring.AnalyzerReferences.Should().ContainSingle();
            siblingWiring.Twgov001IsError.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(siblingRoot))
            {
                Directory.Delete(siblingRoot, recursive: true);
            }

            if (Directory.Exists(siblingParent) && !Directory.EnumerateFileSystemEntries(siblingParent).Any())
            {
                Directory.Delete(siblingParent);
            }
        }
    }

    /// <summary>
    /// 验证仓库品牌分析器能够被当前 SDK 加载并阻断违规声明
    /// </summary>
    [Fact]
    public void RepositoryBrandAnalyzer_RejectsForbiddenDeclarationDuringBuild()
    {
        var dotnetRoot = Path.Combine(FindRepositoryRoot(), "backend", "dotnet");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests");
        var probeDirectory = Path.Combine(testRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(probeDirectory);

        try
        {
            File.WriteAllText(
                Path.Combine(probeDirectory, "BrandProbe.csproj"),
                $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <AssemblyName>BrandProbe</AssemblyName>
                  </PropertyGroup>
                  <Import Project="{{Path.Combine(dotnetRoot, "Directory.Build.props")}}" />
                  <Import Project="{{Path.Combine(dotnetRoot, "Directory.Build.targets")}}" />
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(probeDirectory, "BrandProbe.cs"),
                "public sealed class TwBrandProbe { }");

            var result = RunDotNet(probeDirectory, "build", "BrandProbe.csproj", "--nologo");

            result.ExitCode.Should().NotBe(0);
            result.CombinedOutput.Should().Contain("TWGOV001");
        }
        finally
        {
            var resolvedTestRoot = Path.GetFullPath(testRoot).TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var resolvedProbeDirectory = Path.GetFullPath(probeDirectory).TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            resolvedProbeDirectory
                .StartsWith(resolvedTestRoot, StringComparison.OrdinalIgnoreCase)
                .Should()
                .BeTrue();
            Directory.Delete(probeDirectory, recursive: true);
        }
    }

    /// <summary>
    /// 验证仓库使用单一可覆盖的预发布版本输入
    /// </summary>
    [Fact]
    public void RepositoryVersioning_UsesSingleOverrideablePrereleaseInput()
    {
        var dotnetRoot = Path.Combine(FindRepositoryRoot(), "backend", "dotnet");
        var buildProperties = XDocument.Load(Path.Combine(dotnetRoot, "Directory.Build.props"));
        var packageProperties = XDocument.Load(Path.Combine(dotnetRoot, "Directory.Packages.props"));
        var internalPackages = XDocument.Load(Path.Combine(dotnetRoot, "Build", "Packages.Internal.props"));
        var expectedInternalPackages = new[]
        {
            "Tw.Gateway",
            "Tw.Gateway.Yarp",
            "Tw.AspNetCore",
            "Tw.Observability",
            "Tw.Configuration"
        };

        var versionInputs = buildProperties.Descendants("TwPackageVersion").ToArray();
        versionInputs.Should().ContainSingle();
        var versionInput = versionInputs.Single();
        versionInput.Value.Should().Be("0.1.0-alpha.1");
        versionInput.Attribute("Condition")?.Value.Should().Contain("$(TwPackageVersion)");
        var evaluatedPackageVersions = buildProperties.Descendants("PackageVersion").ToArray();
        evaluatedPackageVersions.Should().ContainSingle();
        evaluatedPackageVersions.Single().Value.Should().Be("$(TwPackageVersion)");
        packageProperties
            .Descendants("CentralPackageFloatingVersionsEnabled")
            .Single()
            .Value
            .Should()
            .Be("false");

        var packageVersions = internalPackages.Descendants("PackageVersion").ToArray();
        packageVersions
            .Select(package => package.Attribute("Include")?.Value)
            .Should()
            .BeEquivalentTo(expectedInternalPackages);
        packageVersions.Should().OnlyContain(package =>
            package.Attribute("Version") != null
            && string.Equals(
                package.Attribute("Version")!.Value,
                "$(TwPackageVersion)",
                StringComparison.Ordinal));

        Directory
            .GetFiles(dotnetRoot, "*.props", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .Should()
            .NotContain(text => text.Contains("GitVersion.MsBuild", StringComparison.Ordinal));
    }

    /// <summary>
    /// 验证网关模板在中央包管理和独立模式下评估互斥的包引用
    /// </summary>
    /// <param name="centralPackageManagementEnabled">是否启用中央包管理</param>
    /// <param name="expectedVersion">评估后的包引用版本</param>
    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "9.8.7-preview.4")]
    public void GatewayTemplate_EvaluatesMutuallyExclusivePackageFallbacks(
        bool centralPackageManagementEnabled,
        string? expectedVersion)
    {
        var projectFile = Path.Combine(
            FindToolRoot(),
            "src",
            "Tw.Templates",
            "content",
            "gateway",
            "src",
            "Company.Gateway.Host",
            "Company.Gateway.Host.csproj");
        var packageReferences = EvaluatePackageReferences(
            projectFile,
            centralPackageManagementEnabled,
            "9.8.7-preview.4");
        var expectedPackageIds = new[]
        {
            "Tw.Gateway",
            "Tw.Gateway.Yarp",
            "Tw.AspNetCore",
            "Tw.Observability",
            "Tw.Configuration"
        };

        packageReferences
            .Select(package => package.PackageId)
            .Should()
            .BeEquivalentTo(expectedPackageIds);
        packageReferences.Should().OnlyContain(package =>
            string.Equals(package.Version, expectedVersion, StringComparison.Ordinal));
    }

    /// <summary>
    /// 验证网关模板公开可覆盖的框架版本参数
    /// </summary>
    [Fact]
    public void GatewayTemplate_ProvidesFrameworkVersionParameter()
    {
        var configurationPath = Path.Combine(
            FindToolRoot(),
            "src",
            "Tw.Templates",
            "content",
            "gateway",
            ".template.config",
            "template.json");
        using var configuration = JsonDocument.Parse(File.ReadAllText(configurationPath));
        configuration.RootElement
            .TryGetProperty("symbols", out var symbols)
            .Should()
            .BeTrue();
        symbols
            .TryGetProperty("frameworkVersion", out var frameworkVersion)
            .Should()
            .BeTrue();
        frameworkVersion.GetProperty("defaultValue").GetString().Should().Be("0.1.0-alpha.1");
    }

    /// <summary>
    /// 验证包消费与模板实例化脚本声明计划要求的输入和隔离边界
    /// </summary>
    [Fact]
    public void VerificationScripts_DeclareRequiredInputsAndIsolationBoundaries()
    {
        var scriptsRoot = Path.Combine(FindToolRoot(), "scripts");
        var packageConsumptionPath = Path.Combine(scriptsRoot, "Test-PackageConsumption.ps1");
        var templateInstantiationPath = Path.Combine(scriptsRoot, "Test-TemplateInstantiation.ps1");

        File.Exists(packageConsumptionPath).Should().BeTrue();
        File.Exists(templateInstantiationPath).Should().BeTrue();

        var packageConsumption = File.ReadAllText(packageConsumptionPath);
        packageConsumption.Should().Contain("[string]$Version");
        packageConsumption.Should().Contain("[string]$OutputDirectory");
        packageConsumption.Should().Contain("building-blocks-topology.json");
        packageConsumption.Should().Contain("runtimeProjects");
        packageConsumption.Should().Contain("retiredPackages");
        packageConsumption.Should().Contain("runs");
        packageConsumption.Should().Contain("feed");

        var templateInstantiation = File.ReadAllText(templateInstantiationPath);
        templateInstantiation.Should().Contain("[string]$TemplatePackage");
        templateInstantiation.Should().Contain("[string]$PackageSource");
        templateInstantiation.Should().Contain("[string]$Version");
        templateInstantiation.Should().Contain("--debug:custom-hive");
        templateInstantiation.Should().Contain("tw-service");
        templateInstantiation.Should().Contain("tw-gateway");
        templateInstantiation.Should().Contain("tw-building-block");
        templateInstantiation.Should().Contain(".template-smoke");
    }

    /// <summary>
    /// 验证生产清理逻辑拒绝 reparse 跳转并只删除本次运行目录
    /// </summary>
    /// <param name="scriptName">待执行清理函数的验证脚本名称</param>
    [Theory]
    [InlineData("Test-PackageConsumption.ps1")]
    [InlineData("Test-TemplateInstantiation.ps1")]
    public void VerificationScripts_CleanupRejectsReparsePointsAndPreservesCallerData(string scriptName)
    {
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", scriptName);
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var controlledRoot = Path.Combine(testRoot, "controlled");
        var safeParent = Path.Combine(controlledRoot, "safe");
        var externalRoot = Path.Combine(testRoot, "external");
        var existingExternalChild = Path.Combine(externalRoot, "existing");
        var callerOutput = Path.Combine(controlledRoot, "output");
        var callerFeed = Path.Combine(callerOutput, "feed");
        var legitimateRun = Path.Combine(callerOutput, "runs", "current");
        var parentLink = Path.Combine(controlledRoot, "parent-link");
        var childLink = Path.Combine(safeParent, "child-link");
        var ancestorLink = Path.Combine(safeParent, "ancestor-link");
        Directory.CreateDirectory(safeParent);
        Directory.CreateDirectory(existingExternalChild);
        Directory.CreateDirectory(callerFeed);
        Directory.CreateDirectory(legitimateRun);
        var sentinelPath = Path.Combine(externalRoot, "sentinel.txt");
        var feedMarkerPath = Path.Combine(callerFeed, "feed-marker.txt");
        File.WriteAllText(sentinelPath, "external-sentinel");
        File.WriteAllText(feedMarkerPath, "caller-feed");

        try
        {
            var reparseFailure = TryCreateDirectoryReparsePoint(parentLink, externalRoot)
                ?? TryCreateDirectoryReparsePoint(childLink, externalRoot)
                ?? TryCreateDirectoryReparsePoint(ancestorLink, externalRoot);
            if (reparseFailure is not null)
            {
                Assert.Skip($"当前平台无法创建目录 reparse point：{reparseFailure}");
            }

            var harness = CreatePowerShellHarness(
                scriptPath,
                ["Resolve-FullPath", "Assert-NoReparsePoint", "Assert-ChildPath", "Remove-ControlledChild"],
                $$"""
                $cases = @(
                    [pscustomobject]@{ Name = 'parent-self'; Parent = {{PowerShellLiteral(parentLink)}}; Child = {{PowerShellLiteral(Path.Combine(parentLink, "missing"))}} },
                    [pscustomobject]@{ Name = 'child-self'; Parent = {{PowerShellLiteral(safeParent)}}; Child = {{PowerShellLiteral(childLink)}} },
                    [pscustomobject]@{ Name = 'existing-ancestor'; Parent = {{PowerShellLiteral(safeParent)}}; Child = {{PowerShellLiteral(Path.Combine(ancestorLink, "existing"))}} },
                    [pscustomobject]@{ Name = 'missing-child-under-ancestor'; Parent = {{PowerShellLiteral(safeParent)}}; Child = {{PowerShellLiteral(Path.Combine(ancestorLink, "missing", "child"))}} }
                )
                foreach ($case in $cases) {
                    try {
                        Remove-ControlledChild -Parent $case.Parent -Child $case.Child
                        throw "cleanup accepted reparse case $($case.Name)"
                    }
                    catch {
                        if ($_.Exception.Message -notlike '*Refusing to operate through a reparse point*') {
                            throw
                        }
                    }
                }

                Remove-ControlledChild -Parent {{PowerShellLiteral(Path.Combine(callerOutput, "runs"))}} -Child {{PowerShellLiteral(legitimateRun)}}
                if (Test-Path -LiteralPath {{PowerShellLiteral(legitimateRun)}}) {
                    throw 'legitimate run directory was not removed'
                }
                if (-not (Test-Path -LiteralPath {{PowerShellLiteral(feedMarkerPath)}} -PathType Leaf)) {
                    throw 'caller feed marker was removed'
                }
                """);

            var result = RunPowerShell(testRoot, harness);

            result.ExitCode.Should().Be(0, result.CombinedOutput);
            File.ReadAllText(sentinelPath).Should().Be("external-sentinel");
            File.ReadAllText(feedMarkerPath).Should().Be("caller-feed");
        }
        finally
        {
            DeleteDirectoryReparsePoint(parentLink);
            DeleteDirectoryReparsePoint(childLink);
            DeleteDirectoryReparsePoint(ancestorLink);
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证模板清理在首个清理失败后仍尝试第二个路径，并聚合主失败与全部清理诊断
    /// </summary>
    [Fact]
    public void TemplateInstantiation_CleanupAttemptsBothPathsAndAggregatesPrimaryDiagnostics()
    {
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-TemplateInstantiation.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var fakeDotNetRoot = Path.Combine(testRoot, "fake-dotnet");
        var capturePath = Path.Combine(testRoot, "dotnet-invocations.log");
        Directory.CreateDirectory(testRoot);
        Directory.CreateDirectory(fakeDotNetRoot);

        try
        {
            CreateArgumentCapturingFailingFakeDotNet(fakeDotNetRoot, exitCode: 41);
            var harness = CreatePowerShellHarness(
                scriptPath,
                ["Invoke-DotNet", "Complete-TemplateCleanup", "Complete-TemplateRun"],
                """
                $attempts = [System.Collections.Generic.List[string]]::new()
                function Remove-ControlledChild {
                    param([string]$Parent, [string]$Child)
                    $attempts.Add($Child)
                    throw "cleanup failed: $Child"
                }

                $primaryError = $null
                try {
                    Invoke-DotNet -Arguments @('new', 'native-primary-probe')
                }
                catch {
                    $primaryError = $_
                }

                try {
                    Complete-TemplateRun `
                        -BuildingSmokeParent 'building-parent' `
                        -BuildingSmokeRoot 'building-child' `
                        -TemporaryParent 'temporary-parent' `
                        -RunRoot 'temporary-child' `
                        -PrimaryError $primaryError
                    throw 'cleanup unexpectedly succeeded'
                }
                catch {
                    if ($_.Exception -isnot [System.AggregateException]) {
                        throw "expected AggregateException, got $($_.Exception.GetType().FullName): $($_.Exception.Message)"
                    }

                    $messages = @($_.Exception.InnerExceptions | ForEach-Object Message)
                    if ($messages.Count -ne 3 -or
                        $messages[0] -notlike '*Native command failed with exit code 41: dotnet new native-primary-probe*' -or
                        $messages[1] -notlike '*cleanup failed: building-child*' -or
                        $messages[2] -notlike '*cleanup failed: temporary-child*') {
                        throw "aggregate did not preserve ordered diagnostics: $($messages -join ' | ')"
                    }
                }

                if ($attempts.Count -ne 2 -or
                    $attempts[0] -cne 'building-child' -or
                    $attempts[1] -cne 'temporary-child') {
                    throw "cleanup attempts were incomplete or out of order: $($attempts -join ', ')"
                }
                """);
            var result = RunPowerShell(
                testRoot,
                harness,
                new Dictionary<string, string?>
                {
                    ["PATH"] = PrependPath(fakeDotNetRoot),
                    ["TW_CAPTURE_PATH"] = capturePath
                });

            result.ExitCode.Should().Be(0, result.CombinedOutput);
            File.ReadAllLines(capturePath).Should().ContainSingle()
                .Which.Should().Be("new native-primary-probe");
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证完整模板入口捕获原生命令失败、完成清理并返回原始退出码与命令诊断
    /// </summary>
    [Fact]
    public void TemplateInstantiation_FullEntryPreservesNativeExitCodeAfterSuccessfulCleanup()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-TemplateInstantiation.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var fakeDotNetRoot = Path.Combine(testRoot, "fake-dotnet");
        var templatePackage = Path.Combine(testRoot, "Tw.Templates.0.1.0.nupkg");
        var packageSource = Path.Combine(testRoot, "feed");
        var temporaryRoot = Path.Combine(testRoot, "temp");
        var capturePath = Path.Combine(testRoot, "dotnet-invocations.log");
        var buildingSmokeParent = Path.Combine(repositoryRoot, "backend", "dotnet", "BuildingBlocks", ".template-smoke");
        var buildingParentExisted = Directory.Exists(buildingSmokeParent);
        var initialBuildingChildren = buildingParentExisted
            ? Directory.GetFileSystemEntries(buildingSmokeParent).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Directory.CreateDirectory(fakeDotNetRoot);
        Directory.CreateDirectory(packageSource);
        Directory.CreateDirectory(temporaryRoot);
        File.WriteAllText(templatePackage, "fake-template-package");

        try
        {
            CreateArgumentCapturingFailingFakeDotNet(fakeDotNetRoot, exitCode: 37);
            var harness = $$"""
                $ErrorActionPreference = 'Stop'
                & {{PowerShellLiteral(scriptPath)}} `
                    -TemplatePackage {{PowerShellLiteral(templatePackage)}} `
                    -PackageSource {{PowerShellLiteral(packageSource)}} `
                    -Version '0.1.0-native-failure'
                exit $LASTEXITCODE
                """;
            var result = RunPowerShell(
                testRoot,
                harness,
                new Dictionary<string, string?>
                {
                    ["PATH"] = PrependPath(fakeDotNetRoot),
                    ["TW_CAPTURE_PATH"] = capturePath,
                    ["TEMP"] = temporaryRoot,
                    ["TMP"] = temporaryRoot,
                    ["TMPDIR"] = temporaryRoot
                });

            result.ExitCode.Should().Be(37, result.CombinedOutput);
            result.CombinedOutput.Should().Contain("Native command failed with exit code 37");
            result.CombinedOutput.Should().Contain("dotnet new --debug:custom-hive");
            File.ReadAllLines(capturePath).Should().ContainSingle()
                .Which.Should().Contain("new --debug:custom-hive").And.Contain("install");
            var runParent = Path.Combine(temporaryRoot, "Tw.TemplateInstantiation");
            if (Directory.Exists(runParent))
            {
                Directory.GetFileSystemEntries(runParent).Should().BeEmpty();
            }

            if (Directory.Exists(buildingSmokeParent))
            {
                Directory.GetFileSystemEntries(buildingSmokeParent)
                    .Should().BeEquivalentTo(initialBuildingChildren);
            }
        }
        finally
        {
            if (Directory.Exists(buildingSmokeParent))
            {
                foreach (var child in Directory.GetFileSystemEntries(buildingSmokeParent))
                {
                    if (!initialBuildingChildren.Contains(child))
                    {
                        Directory.Delete(child, recursive: true);
                    }
                }

                if (!buildingParentExisted && Directory.GetFileSystemEntries(buildingSmokeParent).Length == 0)
                {
                    Directory.Delete(buildingSmokeParent);
                }
            }

            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证包消费脚本在 output、feed 或 runs 穿过 reparse point 时不会启动 dotnet 或写入外部目录
    /// </summary>
    /// <param name="reparseLocation">reparse point 在受控输出树中的位置</param>
    [Theory]
    [InlineData("output")]
    [InlineData("feed")]
    [InlineData("runs")]
    public void PackageConsumption_RejectsReparseOutputTreeBeforeAnyExternalWrite(
        string reparseLocation)
    {
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-PackageConsumption.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var fakeDotNetRoot = Path.Combine(testRoot, "fake-dotnet");
        var externalRoot = Path.Combine(testRoot, "external");
        var outputRoot = Path.Combine(testRoot, "output");
        var capturePath = Path.Combine(testRoot, "dotnet-calls.txt");
        var sentinelPath = Path.Combine(externalRoot, "sentinel.txt");
        var linkPath = reparseLocation switch
        {
            "output" => outputRoot,
            "feed" => Path.Combine(outputRoot, "feed"),
            "runs" => Path.Combine(outputRoot, "runs"),
            _ => throw new ArgumentOutOfRangeException(nameof(reparseLocation), reparseLocation, "未知 reparse 位置")
        };
        Directory.CreateDirectory(fakeDotNetRoot);
        Directory.CreateDirectory(externalRoot);
        File.WriteAllText(sentinelPath, "caller-owned");
        if (reparseLocation != "output")
        {
            Directory.CreateDirectory(outputRoot);
        }

        var reparseFailure = TryCreateDirectoryReparsePoint(linkPath, externalRoot);
        if (reparseFailure is not null)
        {
            Directory.Delete(testRoot, recursive: true);
            Assert.Skip($"当前平台无法创建目录 reparse point：{reparseFailure}");
        }

        CreateFakeDotNet(fakeDotNetRoot, exitCode: 37);
        try
        {
            var harness = $$"""
                $ErrorActionPreference = 'Stop'
                & {{PowerShellLiteral(scriptPath)}} `
                    -Version '0.1.0-reparse-test' `
                    -OutputDirectory {{PowerShellLiteral(outputRoot)}}
                """;
            var result = RunPowerShell(
                testRoot,
                harness,
                new Dictionary<string, string?>
                {
                    ["PATH"] = PrependPath(fakeDotNetRoot),
                    ["TW_CAPTURE_PATH"] = capturePath
                });

            result.ExitCode.Should().NotBe(0);
            File.Exists(capturePath).Should().BeFalse(
                "不可信输出路径必须在任何 dotnet 子进程启动前被拒绝");
            Directory.GetFileSystemEntries(externalRoot)
                .Should()
                .Equal([sentinelPath], "外部目录不得因受控输出初始化而发生变化");
            File.ReadAllText(sentinelPath).Should().Be("caller-owned");
        }
        finally
        {
            DeleteDirectoryReparsePoint(linkPath);
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证每个源包均在同一隔离作用域内先锁定还原，再使用同一 assets 执行打包
    /// </summary>
    [Fact]
    public void PackageConsumption_PackPathUsesLockedIsolatedRestoreForEveryProject()
    {
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-PackageConsumption.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var fakeDotNetRoot = Path.Combine(testRoot, "fake-dotnet");
        var capturePath = Path.Combine(testRoot, "dotnet-invocations.log");
        var configPath = Path.Combine(testRoot, "NuGet.Config");
        var outputRoot = Path.Combine(testRoot, "output");
        var feedRoot = Path.Combine(outputRoot, "feed");
        var runsRoot = Path.Combine(outputRoot, "runs");
        var globalPackages = Path.Combine(runsRoot, "nuget-packages");
        var projects = new[]
        {
            (Path: Path.Combine(testRoot, "src", "Tw.First", "Tw.First.csproj"), WorkRoot: Path.Combine(runsRoot, "pack", "Tw.First")),
            (Path: Path.Combine(testRoot, "src", "Tw.Second", "Tw.Second.csproj"), WorkRoot: Path.Combine(runsRoot, "pack", "Tw.Second"))
        };
        Directory.CreateDirectory(fakeDotNetRoot);
        Directory.CreateDirectory(feedRoot);
        File.WriteAllText(configPath, "<configuration />");
        File.WriteAllText(Path.Combine(testRoot, "Directory.Build.props"), "<Project />");

        try
        {
            CreateArgumentCapturingFakeDotNet(fakeDotNetRoot);
            var calls = string.Join(
                Environment.NewLine,
                projects.Select(project => $$"""
                    Restore-LockedAndPack `
                        -Project {{PowerShellLiteral(project.Path)}} `
                        -NuGetConfig {{PowerShellLiteral(configPath)}} `
                        -GlobalPackagesFolder {{PowerShellLiteral(globalPackages)}} `
                        -ProjectWorkRoot {{PowerShellLiteral(project.WorkRoot)}} `
                        -OutputRoot {{PowerShellLiteral(outputRoot)}} `
                        -FeedRoot {{PowerShellLiteral(feedRoot)}} `
                        -RunsRoot {{PowerShellLiteral(runsRoot)}} `
                        -Version '0.1.0-isolation-test'
                    """));
            var harness = CreatePowerShellHarness(
                scriptPath,
                [
                    "Resolve-FullPath",
                    "Assert-NoReparsePoint",
                    "Assert-ChildPath",
                    "Assert-PackageOutputTree",
                    "Invoke-WithNuGetPackages",
                    "Invoke-DotNet",
                    "Restore-LockedAndPack"
                ],
                calls);

            var result = RunPowerShell(
                testRoot,
                harness,
                new Dictionary<string, string?>
                {
                    ["PATH"] = PrependPath(fakeDotNetRoot),
                    ["TW_CAPTURE_PATH"] = capturePath
                });

            result.ExitCode.Should().Be(0, result.CombinedOutput);
            var invocations = File.ReadAllLines(capturePath);
            invocations.Should().HaveCount(4);
            for (var index = 0; index < projects.Length; index++)
            {
                var restore = invocations[index * 2];
                var pack = invocations[(index * 2) + 1];
                var isolatedObj = Path.GetFullPath(Path.Combine(projects[index].WorkRoot, "obj"));
                var isolatedBin = Path.GetFullPath(Path.Combine(projects[index].WorkRoot, "bin"));
                var isolationProps = Path.Combine(projects[index].WorkRoot, "Directory.Build.isolated.props");

                restore.Should().StartWith(Path.GetFullPath(globalPackages) + "|");
                restore.Should().Contain("restore").And.Contain(projects[index].Path);
                restore.Should().Contain("--configfile").And.Contain(configPath);
                restore.Should().Contain("--locked-mode").And.NotContain("--force-evaluate");
                restore.Should().Contain("-nodeReuse:false");
                restore.Should().Contain("DirectoryBuildPropsPath=").And.Contain(isolationProps);

                pack.Should().StartWith(Path.GetFullPath(globalPackages) + "|");
                pack.Should().Contain("pack").And.Contain(projects[index].Path);
                pack.Should().Contain("--no-restore").And.Contain("-nodeReuse:false");
                pack.Should().Contain("-o").And.Contain(Path.GetFullPath(feedRoot));
                pack.Should().Contain("TwPackageVersion=0.1.0-isolation-test");
                pack.Should().Contain("PackageVersion=0.1.0-isolation-test");
                pack.Should().Contain("DirectoryBuildPropsPath=").And.Contain(isolationProps);

                var properties = XDocument.Load(isolationProps);
                properties.Descendants("MSBuildProjectExtensionsPath").Single().Value
                    .Should().Contain(isolatedObj).And.Contain("$(MSBuildProjectName)");
                properties.Descendants("BaseIntermediateOutputPath").Single().Value
                    .Should().Contain(isolatedObj).And.Contain("$(MSBuildProjectName)");
                properties.Descendants("BaseOutputPath").Single().Value
                    .Should().Contain(isolatedBin).And.Contain("$(MSBuildProjectName)");
                properties.Descendants("DefaultItemExcludes").Single().Value
                    .Should().Contain("$(MSBuildProjectDirectory)/obj/**");
            }
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证 restore 或 pack 子进程替换 feed 后，下一受信边界会拒绝继续使用该路径
    /// </summary>
    /// <param name="swapStage">替换 feed 的 fake dotnet 子命令</param>
    [Theory]
    [InlineData("restore")]
    [InlineData("pack")]
    public void PackageConsumption_RejectsFeedReplacementAtExternalProcessBoundaries(string swapStage)
    {
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-PackageConsumption.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var fakeDotNetRoot = Path.Combine(testRoot, "fake-dotnet");
        var outputRoot = Path.Combine(testRoot, "output");
        var feedRoot = Path.Combine(outputRoot, "feed");
        var runsRoot = Path.Combine(outputRoot, "runs");
        var workRoot = Path.Combine(runsRoot, "current", "pack", "Tw.BoundaryProbe");
        var externalRoot = Path.Combine(testRoot, "external");
        var capturePath = Path.Combine(testRoot, "dotnet-invocations.log");
        var sentinelPath = Path.Combine(externalRoot, "sentinel.txt");
        var projectPath = Path.Combine(testRoot, "src", "Tw.BoundaryProbe.csproj");
        var configPath = Path.Combine(testRoot, "NuGet.Config");
        var globalPackages = Path.Combine(runsRoot, "current", "nuget-packages");
        Directory.CreateDirectory(fakeDotNetRoot);
        Directory.CreateDirectory(feedRoot);
        Directory.CreateDirectory(workRoot);
        Directory.CreateDirectory(externalRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
        File.WriteAllText(Path.Combine(testRoot, "Directory.Build.props"), "<Project />");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.WriteAllText(configPath, "<configuration />");
        File.WriteAllText(sentinelPath, "caller-owned");

        try
        {
            CreateFeedSwappingFakeDotNet(fakeDotNetRoot);
            var harness = CreatePowerShellHarness(
                scriptPath,
                [
                    "Resolve-FullPath",
                    "Assert-NoReparsePoint",
                    "Assert-ChildPath",
                    "Assert-PackageOutputTree",
                    "Invoke-WithNuGetPackages",
                    "Invoke-DotNet",
                    "Restore-LockedAndPack"
                ],
                $$"""
                Restore-LockedAndPack `
                    -Project {{PowerShellLiteral(projectPath)}} `
                    -NuGetConfig {{PowerShellLiteral(configPath)}} `
                    -GlobalPackagesFolder {{PowerShellLiteral(globalPackages)}} `
                    -ProjectWorkRoot {{PowerShellLiteral(workRoot)}} `
                    -OutputRoot {{PowerShellLiteral(outputRoot)}} `
                    -FeedRoot {{PowerShellLiteral(feedRoot)}} `
                    -RunsRoot {{PowerShellLiteral(runsRoot)}} `
                    -Version '0.1.0-boundary-test'
                """);
            var result = RunPowerShell(
                testRoot,
                harness,
                new Dictionary<string, string?>
                {
                    ["PATH"] = PrependPath(fakeDotNetRoot),
                    ["TW_CAPTURE_PATH"] = capturePath,
                    ["TW_SWAP_STAGE"] = swapStage,
                    ["TW_FEED_PATH"] = feedRoot,
                    ["TW_EXTERNAL_PATH"] = externalRoot
                });

            result.ExitCode.Should().NotBe(0);
            result.CombinedOutput.Should().Contain("Refusing to operate through a reparse point");
            File.ReadAllLines(capturePath).Should().Equal(
                swapStage == "restore" ? ["restore"] : ["restore", "pack"]);
            Directory.GetFileSystemEntries(externalRoot).Should().Equal([sentinelPath]);
            File.ReadAllText(sentinelPath).Should().Be("caller-owned");
        }
        finally
        {
            DeleteDirectoryReparsePoint(feedRoot);
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证消费项目的每个 restore/build 子进程返回后都会重新验证受控输出树
    /// </summary>
    /// <param name="swapInvocation">把 feed 替换为外部链接的 dotnet 调用序号</param>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void PackageConsumption_RejectsFeedReplacementBetweenConsumerProcesses(int swapInvocation)
    {
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-PackageConsumption.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var fakeDotNetRoot = Path.Combine(testRoot, "fake-dotnet");
        var outputRoot = Path.Combine(testRoot, "output");
        var feedRoot = Path.Combine(outputRoot, "feed");
        var runsRoot = Path.Combine(outputRoot, "runs");
        var externalRoot = Path.Combine(testRoot, "external");
        var capturePath = Path.Combine(testRoot, "dotnet-invocations.log");
        var counterPath = Path.Combine(testRoot, "dotnet-counter.txt");
        var sentinelPath = Path.Combine(externalRoot, "sentinel.txt");
        Directory.CreateDirectory(fakeDotNetRoot);
        Directory.CreateDirectory(feedRoot);
        Directory.CreateDirectory(runsRoot);
        Directory.CreateDirectory(externalRoot);
        File.WriteAllText(sentinelPath, "caller-owned");

        try
        {
            CreateInvocationFeedSwappingFakeDotNet(fakeDotNetRoot);
            var harness = CreatePowerShellHarness(
                scriptPath,
                [
                    "Resolve-FullPath",
                    "Assert-NoReparsePoint",
                    "Assert-ChildPath",
                    "Assert-PackageOutputTree",
                    "Invoke-WithNuGetPackages",
                    "Invoke-DotNet",
                    "Restore-LockedAndBuild"
                ],
                $$"""
                Restore-LockedAndBuild `
                    -Project {{PowerShellLiteral(Path.Combine(runsRoot, "Consumer.csproj"))}} `
                    -NuGetConfig {{PowerShellLiteral(Path.Combine(runsRoot, "NuGet.Config"))}} `
                    -GlobalPackagesFolder {{PowerShellLiteral(Path.Combine(runsRoot, "nuget-packages"))}} `
                    -OutputRoot {{PowerShellLiteral(outputRoot)}} `
                    -FeedRoot {{PowerShellLiteral(feedRoot)}} `
                    -RunsRoot {{PowerShellLiteral(runsRoot)}}
                """);
            var result = RunPowerShell(
                testRoot,
                harness,
                new Dictionary<string, string?>
                {
                    ["PATH"] = PrependPath(fakeDotNetRoot),
                    ["TW_CAPTURE_PATH"] = capturePath,
                    ["TW_COUNTER_PATH"] = counterPath,
                    ["TW_SWAP_INVOCATION"] = swapInvocation.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["TW_FEED_PATH"] = feedRoot,
                    ["TW_EXTERNAL_PATH"] = externalRoot
                });

            result.ExitCode.Should().NotBe(0);
            result.CombinedOutput.Should().Contain("Refusing to operate through a reparse point");
            File.ReadAllLines(capturePath).Should().HaveCount(swapInvocation);
            Directory.GetFileSystemEntries(externalRoot).Should().Equal([sentinelPath]);
            File.ReadAllText(sentinelPath).Should().Be("caller-owned");
        }
        finally
        {
            DeleteDirectoryReparsePoint(feedRoot);
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证 feed 在打包返回后被替换时，枚举包或打开 nuspec 前都会拒绝扫描
    /// </summary>
    /// <param name="scanStage">受控 feed 的扫描入口</param>
    [Theory]
    [InlineData("enumeration")]
    [InlineData("nuspec")]
    public void PackageConsumption_RejectsFeedReplacementBeforePackageScanning(string scanStage)
    {
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-PackageConsumption.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var outputRoot = Path.Combine(testRoot, "output");
        var feedRoot = Path.Combine(outputRoot, "feed");
        var runsRoot = Path.Combine(outputRoot, "runs");
        var externalRoot = Path.Combine(testRoot, "external");
        var packageName = "Tw.ScanProbe.0.1.0.nupkg";
        var sentinelPath = Path.Combine(externalRoot, packageName);
        Directory.CreateDirectory(feedRoot);
        Directory.CreateDirectory(runsRoot);
        Directory.CreateDirectory(externalRoot);
        File.WriteAllText(sentinelPath, "caller-owned-not-a-zip");
        Directory.Delete(feedRoot);

        try
        {
            var linkFailure = TryCreateDirectoryReparsePoint(feedRoot, externalRoot);
            linkFailure.Should().BeNull(linkFailure);
            var trustedFunction = scanStage == "enumeration"
                ? "Get-ControlledFeedPackages"
                : "Get-ControlledNuspecDependencyIds";
            var body = scanStage == "enumeration"
                ? $$"""
                    Get-ControlledFeedPackages `
                        -OutputRoot {{PowerShellLiteral(outputRoot)}} `
                        -FeedRoot {{PowerShellLiteral(feedRoot)}} `
                        -RunsRoot {{PowerShellLiteral(runsRoot)}} | Out-Null
                    """
                : $$"""
                    Get-ControlledNuspecDependencyIds `
                        -PackagePath {{PowerShellLiteral(Path.Combine(feedRoot, packageName))}} `
                        -OutputRoot {{PowerShellLiteral(outputRoot)}} `
                        -FeedRoot {{PowerShellLiteral(feedRoot)}} `
                        -RunsRoot {{PowerShellLiteral(runsRoot)}} | Out-Null
                    """;
            var harness = CreatePowerShellHarness(
                scriptPath,
                [
                    "Resolve-FullPath",
                    "Assert-NoReparsePoint",
                    "Assert-ChildPath",
                    "Assert-PackageOutputTree",
                    "Get-NuspecDependencyIds",
                    trustedFunction
                ],
                body);

            var result = RunPowerShell(testRoot, harness);

            result.ExitCode.Should().NotBe(0);
            result.CombinedOutput.Should().Contain("Refusing to operate through a reparse point");
            Directory.GetFileSystemEntries(externalRoot).Should().Equal([sentinelPath]);
            File.ReadAllText(sentinelPath).Should().Be("caller-owned-not-a-zip");
        }
        finally
        {
            DeleteDirectoryReparsePoint(feedRoot);
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证真实运行时项目能够只依赖运行目录中的干净 assets 完成无还原打包
    /// </summary>
    [Fact]
    public void PackageConsumption_RealProjectPacksFromCleanIsolatedAssets()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-PackageConsumption.ps1");
        var projectPath = Path.Combine(
            repositoryRoot,
            "backend",
            "dotnet",
            "BuildingBlocks",
            "src",
            "Foundation",
            "Tw.Core",
            "Tw.Core.csproj");
        var repositoryAssets = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "project.assets.json");
        var assetsExisted = File.Exists(repositoryAssets);
        var originalAssets = assetsExisted ? File.ReadAllBytes(repositoryAssets) : null;
        var originalWriteTime = assetsExisted ? File.GetLastWriteTimeUtc(repositoryAssets) : default;
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var runsRoot = Path.Combine(testRoot, "run");
        var projectWorkRoot = Path.Combine(runsRoot, "pack", "Tw.Core");
        var feedRoot = Path.Combine(testRoot, "feed");
        var defaultPackages = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget",
            "packages");
        Directory.CreateDirectory(feedRoot);

        try
        {
            var harness = CreatePowerShellHarness(
                scriptPath,
                [
                    "Resolve-FullPath",
                    "Assert-NoReparsePoint",
                    "Assert-ChildPath",
                    "Assert-PackageOutputTree",
                    "Invoke-WithNuGetPackages",
                    "Invoke-DotNet",
                    "Restore-LockedAndPack"
                ],
                $$"""
                Restore-LockedAndPack `
                    -Project {{PowerShellLiteral(projectPath)}} `
                    -NuGetConfig {{PowerShellLiteral(Path.Combine(repositoryRoot, "backend", "dotnet", "NuGet.Config"))}} `
                    -GlobalPackagesFolder {{PowerShellLiteral(defaultPackages)}} `
                    -ProjectWorkRoot {{PowerShellLiteral(projectWorkRoot)}} `
                    -OutputRoot {{PowerShellLiteral(testRoot)}} `
                    -FeedRoot {{PowerShellLiteral(feedRoot)}} `
                    -RunsRoot {{PowerShellLiteral(runsRoot)}} `
                    -Version '0.1.0-isolated-real-test'
                """);

            var result = RunPowerShell(testRoot, harness);

            result.ExitCode.Should().Be(0, result.CombinedOutput);
            File.Exists(Path.Combine(projectWorkRoot, "obj", "Tw.Core", "project.assets.json"))
                .Should().BeTrue("真实项目的 NuGet assets 必须写入本次运行目录");
            File.Exists(Path.Combine(feedRoot, "Tw.Core.0.1.0-isolated-real-test.nupkg"))
                .Should().BeTrue("pack 必须复用隔离 restore 生成的 assets");
            File.Exists(repositoryAssets).Should().Be(assetsExisted);
            if (assetsExisted)
            {
                File.ReadAllBytes(repositoryAssets).Should().Equal(originalAssets!);
                File.GetLastWriteTimeUtc(repositoryAssets).Should().Be(originalWriteTime);
            }
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证真实 restore/build 路径覆盖污染缓存并在成功后恢复原值
    /// </summary>
    /// <param name="scriptName">待执行 restore/build 函数的验证脚本名称</param>
    [Theory]
    [InlineData("Test-PackageConsumption.ps1")]
    [InlineData("Test-TemplateInstantiation.ps1")]
    public void VerificationScripts_RestorePathScopesNuGetPackagesAndRestoresAfterSuccess(string scriptName)
    {
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", scriptName);
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var fakeDotNetRoot = Path.Combine(testRoot, "fake-dotnet");
        var capturePath = Path.Combine(testRoot, "nuget-packages.log");
        var pollutedPackages = Path.Combine(testRoot, "polluted-packages");
        var runLocalPackages = Path.Combine(testRoot, "run-local", "nuget-packages");
        var isPackageScript = scriptName == "Test-PackageConsumption.ps1";
        var feedRoot = Path.Combine(testRoot, "feed");
        var runsRoot = Path.Combine(testRoot, "run-local");
        Directory.CreateDirectory(fakeDotNetRoot);
        Directory.CreateDirectory(feedRoot);
        Directory.CreateDirectory(runsRoot);

        try
        {
            CreateFakeDotNet(fakeDotNetRoot, exitCode: 0);
            var functionNames = isPackageScript
                ? new[]
                {
                    "Resolve-FullPath",
                    "Assert-NoReparsePoint",
                    "Assert-ChildPath",
                    "Assert-PackageOutputTree",
                    "Invoke-WithNuGetPackages",
                    "Invoke-DotNet",
                    "Restore-LockedAndBuild"
                }
                : new[] { "Resolve-FullPath", "Invoke-WithNuGetPackages", "Invoke-DotNet", "Restore-LockedAndBuild" };
            var packageBoundaryArguments = isPackageScript
                ? $$"""
                    `
                        -OutputRoot {{PowerShellLiteral(testRoot)}} `
                        -FeedRoot {{PowerShellLiteral(feedRoot)}} `
                        -RunsRoot {{PowerShellLiteral(runsRoot)}}
                    """
                : string.Empty;
            var harness = CreatePowerShellHarness(
                scriptPath,
                functionNames,
                $$"""
                [Environment]::SetEnvironmentVariable('NUGET_PACKAGES', {{PowerShellLiteral(pollutedPackages)}}, 'Process')
                try {
                    Restore-LockedAndBuild `
                        -Project {{PowerShellLiteral(Path.Combine(testRoot, "Consumer.csproj"))}} `
                        -NuGetConfig {{PowerShellLiteral(Path.Combine(testRoot, "NuGet.Config"))}} `
                        -GlobalPackagesFolder {{PowerShellLiteral(runLocalPackages)}}{{packageBoundaryArguments}}
                    $restored = [Environment]::GetEnvironmentVariable('NUGET_PACKAGES', 'Process')
                    if ($restored -cne {{PowerShellLiteral(pollutedPackages)}}) {
                        throw "NUGET_PACKAGES was not restored after success: $restored"
                    }
                }
                finally {
                    [Environment]::SetEnvironmentVariable('NUGET_PACKAGES', $null, 'Process')
                }
                """);

            var result = RunPowerShell(
                testRoot,
                harness,
                new Dictionary<string, string?>
                {
                    ["PATH"] = PrependPath(fakeDotNetRoot),
                    ["TW_CAPTURE_PATH"] = capturePath
                });

            result.ExitCode.Should().Be(0, result.CombinedOutput);
            File.ReadAllLines(capturePath)
                .Should()
                .HaveCount(3)
                .And.OnlyContain(value => string.Equals(
                    value,
                    Path.GetFullPath(runLocalPackages),
                    StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证环境作用域在动作失败后精确恢复不存在与非空值状态
    /// </summary>
    /// <param name="scriptName">待执行环境作用域函数的验证脚本名称</param>
    [Theory]
    [InlineData("Test-PackageConsumption.ps1")]
    [InlineData("Test-TemplateInstantiation.ps1")]
    public void VerificationScripts_NuGetScopeRestoresOriginalStateAfterFailure(string scriptName)
    {
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", scriptName);
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var runLocalPackages = Path.Combine(testRoot, "run-local", "nuget-packages");
        Directory.CreateDirectory(testRoot);

        try
        {
            var cases = new (string Name, bool Exists, string? Value)[]
            {
                ("missing", false, null),
                ("non-empty", true, Path.Combine(testRoot, "polluted"))
            };
            foreach (var state in cases)
            {
                var expectedValue = state.Value is null ? "$null" : PowerShellLiteral(state.Value);
                var harness = CreatePowerShellHarness(
                    scriptPath,
                    ["Resolve-FullPath", "Invoke-WithNuGetPackages"],
                    $$"""
                    $beforeVariables = [Environment]::GetEnvironmentVariables('Process')
                    $beforeExists = $beforeVariables.Contains('NUGET_PACKAGES')
                    $beforeValue = [Environment]::GetEnvironmentVariable('NUGET_PACKAGES', 'Process')
                    if ($beforeExists -ne ${{state.Exists.ToString().ToLowerInvariant()}} -or $beforeValue -cne {{expectedValue}}) {
                        throw "test process did not receive the expected {{state.Name}} environment state: exists=$beforeExists value=[$beforeValue]"
                    }
                    try {
                        Invoke-WithNuGetPackages -GlobalPackagesFolder {{PowerShellLiteral(runLocalPackages)}} -Action {
                            $inside = [Environment]::GetEnvironmentVariable('NUGET_PACKAGES', 'Process')
                            if ($inside -cne {{PowerShellLiteral(Path.GetFullPath(runLocalPackages))}}) {
                                throw "scope did not expose run-local packages: $inside"
                            }
                            throw 'expected action failure'
                        }
                        throw 'scope did not propagate action failure'
                    }
                    catch {
                        if ($_.Exception.Message -notlike '*expected action failure*') {
                            throw
                        }
                    }

                    $afterVariables = [Environment]::GetEnvironmentVariables('Process')
                    $afterExists = $afterVariables.Contains('NUGET_PACKAGES')
                    $afterValue = [Environment]::GetEnvironmentVariable('NUGET_PACKAGES', 'Process')
                    if ($afterExists -ne $beforeExists -or $afterValue -cne $beforeValue) {
                        throw 'environment state was not restored for {{state.Name}}'
                    }
                    """);

                var result = RunPowerShell(
                    testRoot,
                    harness,
                    new Dictionary<string, string?>
                    {
                        ["NUGET_PACKAGES"] = state.Exists ? state.Value : null
                    });

                result.ExitCode.Should().Be(0, result.CombinedOutput);
            }
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证完整生产脚本将隔离缓存、charter 校验和受控清理接到顶层控制流
    /// </summary>
    /// <param name="scriptName">待验证的生产脚本名称</param>
    /// <param name="wiringKind">脚本接线策略</param>
    [Theory]
    [InlineData("Test-PackageConsumption.ps1", "package")]
    [InlineData("Test-TemplateInstantiation.ps1", "template")]
    public void VerificationScripts_TopLevelWiringMatchesIsolationAndCleanupPolicy(
        string scriptName,
        string wiringKind)
    {
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", scriptName);
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);

        try
        {
            var harness = CreatePowerShellWiringHarness(scriptPath, wiringKind, verifyMutations: false);
            var result = RunPowerShell(testRoot, harness);

            result.ExitCode.Should().Be(0, result.CombinedOutput);
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证完整脚本接线校验器能够拒绝按 AST 定位的断线 mutation
    /// </summary>
    /// <param name="scriptName">待 mutation 的生产脚本名称</param>
    /// <param name="wiringKind">脚本接线策略</param>
    [Theory]
    [InlineData("Test-PackageConsumption.ps1", "package")]
    [InlineData("Test-TemplateInstantiation.ps1", "template")]
    public void VerificationScripts_WiringValidatorRejectsAstMutations(
        string scriptName,
        string wiringKind)
    {
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", scriptName);
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);

        try
        {
            var harness = CreatePowerShellWiringHarness(scriptPath, wiringKind, verifyMutations: true);
            var result = RunPowerShell(testRoot, harness);

            result.ExitCode.Should().Be(0, result.CombinedOutput);
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证构建块仅在没有内部包引用时使用不受调用者污染的持久缓存
    /// </summary>
    [Fact]
    public void TemplateInstantiation_BuildingBlockPersistentCacheIgnoresCallerNuGetPackages()
    {
        var buildingBlockRoot = Path.Combine(
            FindToolRoot(),
            "src",
            "Tw.Templates",
            "content",
            "building-block");
        var projectFiles = Directory.GetFiles(buildingBlockRoot, "*.csproj", SearchOption.AllDirectories);
        var internalPackageReferences = projectFiles
            .SelectMany(projectFile => XDocument.Load(projectFile).Descendants("PackageReference"))
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(packageId => packageId?.StartsWith("Tw.", StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();
        internalPackageReferences.Should().BeEmpty(
            "持久缓存例外不能让同版本 Tw.* PackageReference 掩盖本次 feed");

        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-TemplateInstantiation.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var pollutedPackages = Path.Combine(testRoot, "polluted-packages");
        Directory.CreateDirectory(testRoot);

        try
        {
            var harness = CreatePowerShellHarness(
                scriptPath,
                ["Resolve-FullPath", "Get-PersistentGlobalPackagesFolder"],
                $$"""
                [Environment]::SetEnvironmentVariable('NUGET_PACKAGES', {{PowerShellLiteral(pollutedPackages)}}, 'Process')
                try {
                    $persistent = Get-PersistentGlobalPackagesFolder
                    $expected = [System.IO.Path]::GetFullPath((Join-Path `
                        ([Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)) `
                        '.nuget/packages'))
                    if ($persistent -cne $expected) {
                        throw "persistent cache was influenced by caller NUGET_PACKAGES: $persistent"
                    }
                    $callerValue = [Environment]::GetEnvironmentVariable('NUGET_PACKAGES', 'Process')
                    if ($callerValue -cne {{PowerShellLiteral(pollutedPackages)}}) {
                        throw "cache selection changed caller NUGET_PACKAGES: $callerValue"
                    }
                }
                finally {
                    [Environment]::SetEnvironmentVariable('NUGET_PACKAGES', $null, 'Process')
                }
                """);

            var result = RunPowerShell(testRoot, harness);

            result.ExitCode.Should().Be(0, result.CombinedOutput);
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证 restore/build 的首个失败子进程以原退出码中止后续调用
    /// </summary>
    /// <param name="scriptName">待执行 restore/build 函数的验证脚本名称</param>
    [Theory]
    [InlineData("Test-PackageConsumption.ps1")]
    [InlineData("Test-TemplateInstantiation.ps1")]
    public void VerificationScripts_RestorePathPropagatesFirstChildFailure(string scriptName)
    {
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", scriptName);
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var fakeDotNetRoot = Path.Combine(testRoot, "fake-dotnet");
        var capturePath = Path.Combine(testRoot, "dotnet-invocations.log");
        var restoredStatePath = Path.Combine(testRoot, "restored-nuget-packages.txt");
        var pollutedPackages = Path.Combine(testRoot, "polluted-packages");
        var runLocalPackages = Path.Combine(testRoot, "run-local", "nuget-packages");
        var isPackageScript = scriptName == "Test-PackageConsumption.ps1";
        var feedRoot = Path.Combine(testRoot, "feed");
        var runsRoot = Path.Combine(testRoot, "run-local");
        Directory.CreateDirectory(fakeDotNetRoot);
        Directory.CreateDirectory(feedRoot);
        Directory.CreateDirectory(runsRoot);

        try
        {
            CreateFakeDotNet(fakeDotNetRoot, exitCode: 37);
            var functionNames = isPackageScript
                ? new[]
                {
                    "Resolve-FullPath",
                    "Assert-NoReparsePoint",
                    "Assert-ChildPath",
                    "Assert-PackageOutputTree",
                    "Invoke-WithNuGetPackages",
                    "Invoke-DotNet",
                    "Restore-LockedAndBuild"
                }
                : new[] { "Resolve-FullPath", "Invoke-WithNuGetPackages", "Invoke-DotNet", "Restore-LockedAndBuild" };
            var packageBoundaryArguments = isPackageScript
                ? $$"""
                    `
                        -OutputRoot {{PowerShellLiteral(testRoot)}} `
                        -FeedRoot {{PowerShellLiteral(feedRoot)}} `
                        -RunsRoot {{PowerShellLiteral(runsRoot)}}
                    """
                : string.Empty;
            var harness = CreatePowerShellHarness(
                scriptPath,
                functionNames,
                $$"""
                [Environment]::SetEnvironmentVariable('NUGET_PACKAGES', {{PowerShellLiteral(pollutedPackages)}}, 'Process')
                $nativeFailureExitCode = $null
                try {
                    Restore-LockedAndBuild `
                        -Project {{PowerShellLiteral(Path.Combine(testRoot, "Consumer.csproj"))}} `
                        -NuGetConfig {{PowerShellLiteral(Path.Combine(testRoot, "NuGet.Config"))}} `
                        -GlobalPackagesFolder {{PowerShellLiteral(runLocalPackages)}}{{packageBoundaryArguments}}
                }
                catch {
                    $nativeFailureExitCode = $_.Exception.Data['NativeExitCode']
                    $nativeCommand = [string]$_.Exception.Data['NativeCommand']
                    if ($nativeFailureExitCode -ne 37 -or $nativeCommand -notlike 'dotnet restore *') {
                        throw "unexpected capturable native failure: exit=$nativeFailureExitCode command=$nativeCommand"
                    }
                }
                finally {
                    $restored = [Environment]::GetEnvironmentVariable('NUGET_PACKAGES', 'Process')
                    [System.IO.File]::WriteAllText({{PowerShellLiteral(restoredStatePath)}}, [string]$restored)
                    [Environment]::SetEnvironmentVariable('NUGET_PACKAGES', $null, 'Process')
                }
                if ($null -ne $nativeFailureExitCode) {
                    exit [int]$nativeFailureExitCode
                }
                """);

            var result = RunPowerShell(
                testRoot,
                harness,
                new Dictionary<string, string?>
                {
                    ["PATH"] = PrependPath(fakeDotNetRoot),
                    ["TW_CAPTURE_PATH"] = capturePath
                });

            result.ExitCode.Should().Be(37, result.CombinedOutput);
            File.ReadAllLines(capturePath).Should().ContainSingle();
            File.ReadAllText(restoredStatePath).Should().Be(pollutedPackages);
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证构建块参数使用简体中文且正式 validator 接受生成后的 charter
    /// </summary>
    [Fact]
    public void TemplateInstantiation_BuildingBlockUsesChineseGovernanceAndOfficialValidator()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-TemplateInstantiation.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var charterPath = Path.Combine(testRoot, "package-charter.yaml");
        Directory.CreateDirectory(testRoot);
        File.WriteAllText(
            charterPath,
            """
            schema_version: "1.0.0"
            package: Tw.TemplateSmoke
            owner: dotnet-framework
            responsibility: 验证构建块模板实例化结果
            in_scope:
              - 验证生成路径和项目引用
            out_of_scope:
              - 不提供基础设施实现
            public_capabilities:
              - Tw.TemplateSmoke
            dependency_rules:
              forbid:
                - "*TestBase"
              allow: []
            stability: experimental
            compatibility: 首个稳定版本发布前允许调整公开能力。
            """);

        try
        {
            var harness = CreatePowerShellHarness(
                scriptPath,
                ["Get-BuildingBlockTemplateArguments", "Resolve-PythonCommand", "Invoke-CharterValidator"],
                $$"""
                $arguments = @(Get-BuildingBlockTemplateArguments -CustomHive 'hive' -OutputDirectory 'output')
                $expected = @{
                    '--responsibility' = '验证构建块模板实例化结果'
                    '--inScope' = '验证生成路径和项目引用'
                    '--outOfScope' = '不提供基础设施实现'
                }
                foreach ($entry in $expected.GetEnumerator()) {
                    $index = [Array]::IndexOf($arguments, $entry.Key)
                    if ($index -lt 0 -or $arguments[$index + 1] -cne $entry.Value) {
                        throw "building-block argument $($entry.Key) did not use the expected Simplified Chinese value"
                    }
                }
                Invoke-CharterValidator -CharterPath {{PowerShellLiteral(charterPath)}} -RepositoryRoot {{PowerShellLiteral(repositoryRoot)}}
                """);

            var result = RunPowerShell(testRoot, harness);

            result.ExitCode.Should().Be(0, result.CombinedOutput);
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证正式 validator 拒绝英文 charter 并传播非零退出码
    /// </summary>
    [Fact]
    public void TemplateInstantiation_OfficialValidatorFailurePropagates()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-TemplateInstantiation.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var charterPath = Path.Combine(testRoot, "invalid-package-charter.yaml");
        Directory.CreateDirectory(testRoot);
        File.WriteAllText(
            charterPath,
            """
            schema_version: "1.0.0"
            package: Tw.TemplateSmoke
            owner: dotnet-framework
            responsibility: Provides template verification
            in_scope:
              - Generated paths
            out_of_scope:
              - Infrastructure implementations
            public_capabilities:
              - Tw.TemplateSmoke
            dependency_rules:
              forbid: []
              allow: []
            """);

        try
        {
            var harness = CreatePowerShellHarness(
                scriptPath,
                ["Resolve-PythonCommand", "Invoke-CharterValidator"],
                $"Invoke-CharterValidator -CharterPath {PowerShellLiteral(charterPath)} -RepositoryRoot {PowerShellLiteral(repositoryRoot)}");

            var result = RunPowerShell(testRoot, harness);

            result.ExitCode.Should().NotBe(0);
            result.CombinedOutput.Should().Contain("responsibility must use Simplified Chinese");
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证隔离 Python 模式不会从工作目录或调用方 PYTHONPATH 加载伪造的 tw_memory
    /// </summary>
    [Fact]
    public void TemplateInstantiation_CharterValidatorRejectsCwdAndPythonPathShadowing()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-TemplateInstantiation.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var maliciousPackage = Path.Combine(testRoot, "tw_memory");
        var charterPath = Path.Combine(
            repositoryRoot,
            "backend",
            "dotnet",
            "BuildingBlocks",
            "src",
            "Foundation",
            "Tw.Core",
            "package-charter.yaml");
        Directory.CreateDirectory(maliciousPackage);
        File.WriteAllText(Path.Combine(maliciousPackage, "__init__.py"), string.Empty);
        File.WriteAllText(
            Path.Combine(maliciousPackage, "charter.py"),
            "raise RuntimeError('malicious cwd tw_memory was imported')\n");

        try
        {
            var harness = CreatePowerShellHarness(
                scriptPath,
                ["Resolve-PythonCommand", "Invoke-CharterValidator"],
                $"Invoke-CharterValidator -CharterPath {PowerShellLiteral(charterPath)} -RepositoryRoot {PowerShellLiteral(repositoryRoot)}");
            var result = RunPowerShell(
                testRoot,
                harness,
                new Dictionary<string, string?> { ["PYTHONPATH"] = testRoot });

            result.ExitCode.Should().Be(0, result.CombinedOutput);
            result.CombinedOutput.Should().NotContain("malicious cwd tw_memory was imported");
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 验证仅有 python3 时能被跨平台发现，并把隔离参数、绝对路径和子进程退出码原样传播
    /// </summary>
    [Fact]
    public void TemplateInstantiation_CharterValidatorSelectsPython3AndThrowsCapturableExactExitCode()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(FindToolRoot(), "scripts", "Test-TemplateInstantiation.ps1");
        var testRoot = Path.Combine(Path.GetTempPath(), "Tw.Templates.Tests", Guid.NewGuid().ToString("N"));
        var fakePythonRoot = Path.Combine(testRoot, "fake-python");
        var capturePath = Path.Combine(testRoot, "python-invocation.log");
        var charterPath = Path.Combine(testRoot, "package-charter.yaml");
        Directory.CreateDirectory(fakePythonRoot);
        File.WriteAllText(charterPath, "schema_version: \"1.0.0\"");

        try
        {
            CreateFakePython3(fakePythonRoot, exitCode: 37);
            var harness = CreatePowerShellHarness(
                scriptPath,
                ["Resolve-PythonCommand", "Invoke-CharterValidator"],
                $$"""
                try {
                    Invoke-CharterValidator `
                        -CharterPath {{PowerShellLiteral(charterPath)}} `
                        -RepositoryRoot {{PowerShellLiteral(repositoryRoot)}}
                    throw 'validator unexpectedly succeeded'
                }
                catch {
                    if ($_.Exception.Data['NativeExitCode'] -ne 37) {
                        throw "validator did not preserve exit code: $($_.Exception.Data['NativeExitCode'])"
                    }

                    $nativeCommand = [string]$_.Exception.Data['NativeCommand']
                    if ($nativeCommand -notlike '*python3* -I -c <charter-validator>*') {
                        throw "validator did not preserve command diagnostics: $nativeCommand"
                    }
                }
                """);
            var result = RunPowerShell(
                testRoot,
                harness,
                new Dictionary<string, string?>
                {
                    ["PATH"] = fakePythonRoot,
                    ["TW_CAPTURE_PATH"] = capturePath
                });

            result.ExitCode.Should().Be(0, result.CombinedOutput);
            File.Exists(capturePath).Should().BeTrue("python3 替身必须被实际调用");
            var invocation = File.ReadAllText(capturePath);
            invocation.Should().Contain("-I").And.Contain("-c");
            invocation.Should().Contain(Path.GetFullPath(Path.Combine(repositoryRoot, "tools", "src")));
            invocation.Should().Contain(Path.GetFullPath(charterPath));
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// 查找工具根目录并返回匹配结果
    /// </summary>
    /// <returns>当前工具源码根目录路径</returns>
    private static string FindToolRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var tools = Path.Combine(directory.FullName, "backend", "dotnet", "tools");
            if (Directory.Exists(tools))
            {
                return tools;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Cannot locate backend/dotnet/tools.");
    }

    /// <summary>
    /// 查找仓库根目录并返回匹配结果
    /// </summary>
    /// <returns>当前仓库根目录路径</returns>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var manifest = Path.Combine(
                directory.FullName,
                "backend",
                "dotnet",
                "BuildingBlocks",
                "building-blocks-topology.json");
            if (File.Exists(manifest))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Cannot locate repository root.");
    }

    /// <summary>
    /// 从拓扑清单读取已退役包标识
    /// </summary>
    /// <param name="repositoryRoot">仓库根目录</param>
    /// <returns>已退役或保留禁用的包标识</returns>
    private static string[] LoadRetiredPackageIds(string repositoryRoot)
    {
        var manifestPath = Path.Combine(
            repositoryRoot,
            "backend",
            "dotnet",
            "BuildingBlocks",
            "building-blocks-topology.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));

        return manifest.RootElement
            .GetProperty("retiredPackages")
            .EnumerateArray()
            .Select(package => package.GetProperty("packageId").GetString())
            .Where(packageId => packageId is not null)
            .Cast<string>()
            .ToArray();
    }

    /// <summary>
    /// 判断包标识是否属于指定包族
    /// </summary>
    /// <param name="packageId">待检查包标识</param>
    /// <param name="packageFamily">包族根标识</param>
    /// <returns>包标识等于根标识或以根标识加点号开头时返回 true</returns>
    private static bool IsPackageFamily(string packageId, string packageFamily)
    {
        return packageId.Equals(packageFamily, StringComparison.OrdinalIgnoreCase)
            || packageId.StartsWith($"{packageFamily}.", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 读取项目文件中的包与项目引用标识
    /// </summary>
    /// <param name="projectFile">待检查项目文件</param>
    /// <returns>项目引用的依赖标识</returns>
    private static IEnumerable<string> ReadProjectPackageIdentities(string projectFile)
    {
        var document = XDocument.Load(projectFile);

        foreach (var packageReference in document.Descendants("PackageReference"))
        {
            var packageId = packageReference.Attribute("Include")?.Value;
            if (!string.IsNullOrWhiteSpace(packageId))
            {
                yield return packageId;
            }
        }

        foreach (var projectReference in document.Descendants("ProjectReference"))
        {
            var projectPath = projectReference.Attribute("Include")?.Value;
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                yield return Path.GetFileNameWithoutExtension(projectPath);
            }
        }
    }

    /// <summary>
    /// 读取 NuGet 锁文件中的直接和传递依赖标识
    /// </summary>
    /// <param name="lockFile">待检查锁文件</param>
    /// <returns>锁定的包标识</returns>
    private static IEnumerable<string> ReadLockPackageIdentities(string lockFile)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(lockFile));
        foreach (var targetFramework in document.RootElement.GetProperty("dependencies").EnumerateObject())
        {
            foreach (var package in targetFramework.Value.EnumerateObject())
            {
                yield return package.Name;

                if (package.Value.TryGetProperty("dependencies", out var dependencies))
                {
                    foreach (var dependency in dependencies.EnumerateObject())
                    {
                        yield return dependency.Name;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 判断项目引用是否由仓库项目引用开关控制
    /// </summary>
    /// <param name="reference">待检查项目引用</param>
    /// <returns>仓库项目引用开关控制时返回 true</returns>
    private static bool IsRepositoryConditionalReference(XElement reference)
    {
        return reference
            .Ancestors("ItemGroup")
            .Any(group => group.Attribute("Condition")?.Value.Contains(
                "UseRepositoryProjectReferences",
                StringComparison.Ordinal) == true);
    }

    /// <summary>
    /// 通过 MSBuild 评估网关模板的包引用
    /// </summary>
    /// <param name="projectFile">待评估的网关项目</param>
    /// <param name="centralPackageManagementEnabled">是否启用中央包管理</param>
    /// <param name="frameworkVersion">独立模式使用的框架版本</param>
    /// <returns>评估后的包标识与版本</returns>
    private static IReadOnlyList<EvaluatedPackageReference> EvaluatePackageReferences(
        string projectFile,
        bool centralPackageManagementEnabled,
        string frameworkVersion)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(projectFile);
        startInfo.ArgumentList.Add("-getItem:PackageReference");
        startInfo.ArgumentList.Add("-p:UseRepositoryProjectReferences=false");
        startInfo.ArgumentList.Add($"-p:ManagePackageVersionsCentrally={centralPackageManagementEnabled.ToString().ToLowerInvariant()}");
        startInfo.ArgumentList.Add($"-p:TwFrameworkVersion={frameworkVersion}");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("无法启动 dotnet msbuild 评估模板项目");
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        process.ExitCode.Should().Be(0, standardError);

        using var evaluation = JsonDocument.Parse(standardOutput);
        return evaluation.RootElement
            .GetProperty("Items")
            .GetProperty("PackageReference")
            .EnumerateArray()
            .Select(package => new EvaluatedPackageReference(
                package.GetProperty("Identity").GetString()!,
                package.TryGetProperty("Version", out var version) ? version.GetString() : null))
            .ToArray();
    }

    /// <summary>
    /// 在指定目录执行 dotnet 命令并捕获完整结果
    /// </summary>
    /// <param name="workingDirectory">命令工作目录</param>
    /// <param name="arguments">dotnet 子命令及参数</param>
    /// <returns>进程退出码和合并输出</returns>
    private static ProcessExecutionResult RunDotNet(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("无法启动 dotnet 命令");
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessExecutionResult(
            process.ExitCode,
            string.Join(Environment.NewLine, standardOutput, standardError));
    }

    /// <summary>
    /// 评估项目是否启用仓库品牌 analyzer 以及自动注入的 analyzer 项目引用
    /// </summary>
    /// <param name="projectPath">待评估项目完整路径</param>
    /// <returns>品牌 analyzer 开关和 analyzer 引用完整路径</returns>
    private static AnalyzerWiringEvaluation EvaluateAnalyzerWiring(
        string projectPath,
        IReadOnlyDictionary<string, string>? globalProperties = null)
    {
        var arguments = new List<string>
        {
            "msbuild",
            projectPath,
            "-nologo",
            "-getProperty:_TwUseRepositoryBrandAnalyzer",
            "-getProperty:WarningsAsErrors",
            "-getItem:ProjectReference",
            "-nodeReuse:false"
        };
        if (globalProperties is not null)
        {
            arguments.AddRange(globalProperties.Select(property => $"-p:{property.Key}={property.Value}"));
        }

        var result = RunDotNet(
            Path.GetDirectoryName(projectPath)!,
            arguments.ToArray());
        result.ExitCode.Should().Be(0, result.CombinedOutput);
        using var document = JsonDocument.Parse(result.CombinedOutput);
        var enabled = string.Equals(
            document.RootElement
                .GetProperty("Properties")
                .GetProperty("_TwUseRepositoryBrandAnalyzer")
                .GetString(),
            "true",
            StringComparison.OrdinalIgnoreCase);
        var analyzerReferences = document.RootElement
            .GetProperty("Items")
            .GetProperty("ProjectReference")
            .EnumerateArray()
            .Where(item => string.Equals(
                item.GetProperty("OutputItemType").GetString(),
                "Analyzer",
                StringComparison.OrdinalIgnoreCase))
            .Select(item => item.GetProperty("FullPath").GetString()!)
            .ToArray();
        var warningsAsErrors = document.RootElement
            .GetProperty("Properties")
            .GetProperty("WarningsAsErrors")
            .GetString() ?? string.Empty;
        var twgov001IsError = warningsAsErrors
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains("TWGOV001", StringComparer.OrdinalIgnoreCase);

        return new AnalyzerWiringEvaluation(enabled, twgov001IsError, analyzerReferences);
    }

    /// <summary>
    /// 仅改变 ASCII 字母大小写，以构造不依赖文件系统能力的路径比较输入
    /// </summary>
    /// <param name="path">原始路径字符串</param>
    /// <returns>ASCII 字母大小写全部翻转后的路径字符串</returns>
    private static string ToggleAsciiPathCase(string path)
    {
        return string.Concat(path.Select(character => character switch
        {
            >= 'a' and <= 'z' => char.ToUpperInvariant(character),
            >= 'A' and <= 'Z' => char.ToLowerInvariant(character),
            _ => character
        }));
    }

    /// <summary>
    /// 创建完整生产脚本的 AST 接线校验 harness
    /// </summary>
    /// <param name="scriptPath">生产 PowerShell 脚本路径</param>
    /// <param name="wiringKind">脚本接线策略</param>
    /// <param name="verifyMutations">是否同时验证 AST 定位 mutation 会被拒绝</param>
    /// <returns>解析完整生产脚本并校验顶层接线的 PowerShell harness</returns>
    /// <exception cref="ArgumentOutOfRangeException">接线策略不受支持时抛出</exception>
    private static string CreatePowerShellWiringHarness(
        string scriptPath,
        string wiringKind,
        bool verifyMutations)
    {
        if (wiringKind is not ("package" or "template"))
        {
            throw new ArgumentOutOfRangeException(nameof(wiringKind), wiringKind, "不支持的脚本接线策略");
        }

        var verifyMutationsLiteral = verifyMutations ? "$true" : "$false";
        return $$"""
            $ErrorActionPreference = 'Stop'

            function Parse-WiringAst {
                param([Parameter(Mandatory = $true)][string]$Source)

                $tokens = $null
                $parseErrors = $null
                $ast = [System.Management.Automation.Language.Parser]::ParseInput(
                    $Source,
                    [ref]$tokens,
                    [ref]$parseErrors)
                if ($parseErrors.Count -ne 0) {
                    throw "wiring source has parse errors: $($parseErrors -join '; ')"
                }

                return $ast
            }

            function Test-IsInsideFunction {
                param([Parameter(Mandatory = $true)]$Node)

                $ancestor = $Node.Parent
                while ($null -ne $ancestor) {
                    if ($ancestor -is [System.Management.Automation.Language.FunctionDefinitionAst]) {
                        return $true
                    }

                    $ancestor = $ancestor.Parent
                }

                return $false
            }

            function Get-ScriptCommands {
                param(
                    [Parameter(Mandatory = $true)]$Ast,
                    [Parameter(Mandatory = $true)][string]$Name)

                return @($Ast.FindAll({
                    param($node)
                    $node -is [System.Management.Automation.Language.CommandAst] -and
                        $node.GetCommandName() -ceq $Name
                }, $true) | Where-Object { -not (Test-IsInsideFunction $_) })
            }

            function Get-CommandParameterArgument {
                param(
                    [Parameter(Mandatory = $true)]$Command,
                    [Parameter(Mandatory = $true)][string]$Name)

                $elements = @($Command.CommandElements)
                for ($index = 1; $index -lt $elements.Count; $index++) {
                    $element = $elements[$index]
                    if ($element -is [System.Management.Automation.Language.CommandParameterAst] -and
                        $element.ParameterName -ceq $Name) {
                        if ($index + 1 -ge $elements.Count -or
                            $elements[$index + 1] -is [System.Management.Automation.Language.CommandParameterAst]) {
                            throw "$($Command.GetCommandName()) -$Name has no AST argument"
                        }

                        return $elements[$index + 1]
                    }
                }

                throw "$($Command.GetCommandName()) is missing -$Name"
            }

            function Test-ArgumentContainsVariable {
                param(
                    [Parameter(Mandatory = $true)]$Argument,
                    [Parameter(Mandatory = $true)][string]$VariableName)

                if ($Argument -is [System.Management.Automation.Language.VariableExpressionAst] -and
                    $Argument.VariablePath.UserPath -ceq $VariableName) {
                    return $true
                }

                $variables = @($Argument.FindAll({
                    param($node)
                    $node -is [System.Management.Automation.Language.VariableExpressionAst] -and
                        $node.VariablePath.UserPath -ceq $VariableName
                }, $true))
                return $variables.Count -gt 0
            }

            function Assert-DirectVariableArgument {
                param(
                    [Parameter(Mandatory = $true)]$Command,
                    [Parameter(Mandatory = $true)][string]$ParameterName,
                    [Parameter(Mandatory = $true)][string]$VariableName)

                $argument = Get-CommandParameterArgument $Command $ParameterName
                if ($argument -isnot [System.Management.Automation.Language.VariableExpressionAst] -or
                    $argument.VariablePath.UserPath -cne $VariableName) {
                    throw "$($Command.GetCommandName()) -$ParameterName must be direct variable `$$VariableName"
                }
            }

            function Assert-PersistentCacheArgument {
                param(
                    [Parameter(Mandatory = $true)]$Command,
                    [Parameter(Mandatory = $true)][string]$ParameterName)

                $argument = Get-CommandParameterArgument $Command $ParameterName
                if ($argument -isnot [System.Management.Automation.Language.ParenExpressionAst]) {
                    throw "$($Command.GetCommandName()) -$ParameterName must call Get-PersistentGlobalPackagesFolder"
                }

                $persistentCalls = @($argument.FindAll({
                    param($node)
                    $node -is [System.Management.Automation.Language.CommandAst] -and
                        $node.GetCommandName() -ceq 'Get-PersistentGlobalPackagesFolder'
                }, $true))
                if ($persistentCalls.Count -ne 1) {
                    throw "$($Command.GetCommandName()) -$ParameterName must contain one persistent-cache call"
                }
            }

            function Get-TopLevelTry {
                param([Parameter(Mandatory = $true)]$Ast)

                $tries = @($Ast.FindAll({
                    param($node)
                    $node -is [System.Management.Automation.Language.TryStatementAst]
                }, $true) | Where-Object { -not (Test-IsInsideFunction $_) })
                if ($tries.Count -ne 1) {
                    throw "script must contain exactly one top-level try/finally, found $($tries.Count)"
                }

                if ($null -eq $tries[0].Finally) {
                    throw 'top-level try must contain finally cleanup'
                }

                return $tries[0]
            }

            function Assert-WithinAst {
                param(
                    [Parameter(Mandatory = $true)]$Node,
                    [Parameter(Mandatory = $true)]$Container,
                    [Parameter(Mandatory = $true)][string]$Description)

                if ($Node.Extent.StartOffset -lt $Container.Extent.StartOffset -or
                    $Node.Extent.EndOffset -gt $Container.Extent.EndOffset) {
                    throw "$Description is outside the required control-flow block"
                }
            }

            function Get-RestoreByProjectVariable {
                param(
                    [Parameter(Mandatory = $true)][object[]]$Commands,
                    [Parameter(Mandatory = $true)][string]$VariableName)

                $matches = @($Commands | Where-Object {
                    $argument = Get-CommandParameterArgument $_ 'Project'
                    Test-ArgumentContainsVariable $argument $VariableName
                })
                if ($matches.Count -ne 1) {
                    throw "expected one Restore-LockedAndBuild for `$$VariableName, found $($matches.Count)"
                }

                return $matches[0]
            }

            function Get-CleanupByChildVariable {
                param(
                    [Parameter(Mandatory = $true)][object[]]$Commands,
                    [Parameter(Mandatory = $true)][string]$VariableName)

                $matches = @($Commands | Where-Object {
                    $argument = Get-CommandParameterArgument $_ 'Child'
                    $argument -is [System.Management.Automation.Language.VariableExpressionAst] -and
                        $argument.VariablePath.UserPath -ceq $VariableName
                })
                if ($matches.Count -ne 1) {
                    throw "expected one controlled cleanup for `$$VariableName, found $($matches.Count)"
                }

                return $matches[0]
            }

            function Assert-PackageWiring {
                param([Parameter(Mandatory = $true)]$Ast)

                $topLevelTry = Get-TopLevelTry $Ast
                $restores = @(Get-ScriptCommands $Ast 'Restore-LockedAndBuild')
                if ($restores.Count -ne 1) {
                    throw "package script must contain one executable Restore-LockedAndBuild, found $($restores.Count)"
                }

                Assert-WithinAst $restores[0] $topLevelTry.Body 'package restore/build'
                Assert-DirectVariableArgument $restores[0] 'Project' 'consumerProject'
                Assert-DirectVariableArgument $restores[0] 'NuGetConfig' 'consumerNuGetConfig'
                Assert-DirectVariableArgument $restores[0] 'GlobalPackagesFolder' 'globalPackagesFolder'
                Assert-DirectVariableArgument $restores[0] 'OutputRoot' 'outputRoot'
                Assert-DirectVariableArgument $restores[0] 'FeedRoot' 'feedRoot'
                Assert-DirectVariableArgument $restores[0] 'RunsRoot' 'runsRoot'

                $packs = @(Get-ScriptCommands $Ast 'Restore-LockedAndPack')
                if ($packs.Count -ne 1) {
                    throw "package script must contain one executable Restore-LockedAndPack, found $($packs.Count)"
                }

                Assert-WithinAst $packs[0] $topLevelTry.Body 'source package locked restore/pack'
                Assert-DirectVariableArgument $packs[0] 'Project' 'projectPath'
                Assert-DirectVariableArgument $packs[0] 'NuGetConfig' 'consumerNuGetConfig'
                Assert-DirectVariableArgument $packs[0] 'GlobalPackagesFolder' 'globalPackagesFolder'
                Assert-DirectVariableArgument $packs[0] 'ProjectWorkRoot' 'packageWorkRoot'
                Assert-DirectVariableArgument $packs[0] 'OutputRoot' 'outputRoot'
                Assert-DirectVariableArgument $packs[0] 'FeedRoot' 'feedRoot'
                Assert-DirectVariableArgument $packs[0] 'RunsRoot' 'runsRoot'
                Assert-DirectVariableArgument $packs[0] 'Version' 'Version'

                $sourceLoop = $null
                $packAncestor = $packs[0].Parent
                while ($null -ne $packAncestor) {
                    if ($packAncestor -is [System.Management.Automation.Language.ForEachStatementAst]) {
                        $collection = $packAncestor.Condition.GetPureExpression()
                        if ($packAncestor.Variable -is [System.Management.Automation.Language.VariableExpressionAst] -and
                            $packAncestor.Variable.VariablePath.UserPath -ceq 'runtimeProject' -and
                            $collection -is [System.Management.Automation.Language.VariableExpressionAst] -and
                            $collection.VariablePath.UserPath -ceq 'runtimeProjects') {
                            $sourceLoop = $packAncestor
                            break
                        }
                    }

                    $packAncestor = $packAncestor.Parent
                }

                if ($null -eq $sourceLoop) {
                    throw 'source package restore/pack must be inside foreach ($runtimeProject in $runtimeProjects)'
                }

                $consumerLoop = $null
                $ancestor = $restores[0].Parent
                while ($null -ne $ancestor) {
                    if ($ancestor -is [System.Management.Automation.Language.ForEachStatementAst]) {
                        $collection = $ancestor.Condition.GetPureExpression()
                        if ($ancestor.Variable -is [System.Management.Automation.Language.VariableExpressionAst] -and
                            $ancestor.Variable.VariablePath.UserPath -ceq 'package' -and
                            $collection -is [System.Management.Automation.Language.VariableExpressionAst] -and
                            $collection.VariablePath.UserPath -ceq 'packages') {
                            $consumerLoop = $ancestor
                            break
                        }
                    }

                    $ancestor = $ancestor.Parent
                }

                if ($null -eq $consumerLoop) {
                    throw 'package restore/build must be inside foreach ($package in $packages)'
                }

                $cleanups = @(Get-ScriptCommands $Ast 'Remove-ControlledChild')
                if ($cleanups.Count -ne 1) {
                    throw "package script must contain one executable controlled cleanup, found $($cleanups.Count)"
                }

                Assert-WithinAst $cleanups[0] $topLevelTry.Finally 'package cleanup'
                Assert-DirectVariableArgument $cleanups[0] 'Parent' 'runsRoot'
                Assert-DirectVariableArgument $cleanups[0] 'Child' 'runRoot'
            }

            function Assert-TemplateWiring {
                param([Parameter(Mandatory = $true)]$Ast)

                $topLevelTry = Get-TopLevelTry $Ast
                $restores = @(Get-ScriptCommands $Ast 'Restore-LockedAndBuild')
                if ($restores.Count -ne 3) {
                    throw "template script must contain three executable Restore-LockedAndBuild calls, found $($restores.Count)"
                }

                $serviceRestore = Get-RestoreByProjectVariable $restores 'serviceRoot'
                $gatewayRestore = Get-RestoreByProjectVariable $restores 'gatewayRoot'
                $buildingRestore = Get-RestoreByProjectVariable $restores 'testProject'
                foreach ($restore in $restores) {
                    Assert-WithinAst $restore $topLevelTry.Body 'template restore/build'
                }

                Assert-DirectVariableArgument $serviceRestore 'GlobalPackagesFolder' 'globalPackagesFolder'
                Assert-DirectVariableArgument $gatewayRestore 'GlobalPackagesFolder' 'globalPackagesFolder'
                Assert-PersistentCacheArgument $buildingRestore 'GlobalPackagesFolder'

                $generationCalls = @(Get-ScriptCommands $Ast 'Get-BuildingBlockTemplateArguments')
                if ($generationCalls.Count -ne 1) {
                    throw "template script must generate one building block, found $($generationCalls.Count)"
                }

                Assert-DirectVariableArgument $generationCalls[0] 'OutputDirectory' 'buildingSmokeRoot'
                Assert-WithinAst $generationCalls[0] $topLevelTry.Body 'building-block generation'
                $generationCommand = $generationCalls[0].Parent
                while ($null -ne $generationCommand -and
                    $generationCommand -isnot [System.Management.Automation.Language.CommandAst]) {
                    $generationCommand = $generationCommand.Parent
                }

                if ($null -eq $generationCommand -or $generationCommand.GetCommandName() -cne 'Invoke-DotNet') {
                    throw 'building-block arguments must be executed by Invoke-DotNet'
                }

                $validators = @(Get-ScriptCommands $Ast 'Invoke-CharterValidator')
                if ($validators.Count -ne 1) {
                    throw "template script must contain one executable charter validator, found $($validators.Count)"
                }

                Assert-WithinAst $validators[0] $topLevelTry.Body 'building-block charter validator'
                Assert-DirectVariableArgument $validators[0] 'CharterPath' 'charterPath'
                Assert-DirectVariableArgument $validators[0] 'RepositoryRoot' 'repositoryRoot'
                if ($generationCommand.Extent.EndOffset -ge $validators[0].Extent.StartOffset -or
                    $validators[0].Extent.EndOffset -ge $buildingRestore.Extent.StartOffset) {
                    throw 'charter validator must run after building-block generation and before its restore/build'
                }

                $cleanups = @(Get-ScriptCommands $Ast 'Complete-TemplateRun')
                if ($cleanups.Count -ne 1) {
                    throw "template script must contain one executable run completion, found $($cleanups.Count)"
                }

                Assert-WithinAst $cleanups[0] $topLevelTry.Finally 'aggregate template cleanup'
                Assert-DirectVariableArgument $cleanups[0] 'BuildingSmokeParent' 'buildingSmokeParent'
                Assert-DirectVariableArgument $cleanups[0] 'BuildingSmokeRoot' 'buildingSmokeRoot'
                Assert-DirectVariableArgument $cleanups[0] 'TemporaryParent' 'temporaryParent'
                Assert-DirectVariableArgument $cleanups[0] 'RunRoot' 'runRoot'
                Assert-DirectVariableArgument $cleanups[0] 'PrimaryError' 'primaryError'

                $resourceCreations = @($Ast.FindAll({
                    param($node)
                    $node -is [System.Management.Automation.Language.InvokeMemberExpressionAst] -and
                        $node.Member.Value -ceq 'CreateDirectory'
                }, $true))
                if ($resourceCreations.Count -ne 3) {
                    throw "template script must contain three resource directory acquisitions, found $($resourceCreations.Count)"
                }

                foreach ($resourceCreation in $resourceCreations) {
                    Assert-WithinAst $resourceCreation $topLevelTry.Body 'template resource acquisition'
                }
            }

            function Assert-Wiring {
                param(
                    [Parameter(Mandatory = $true)]$Ast,
                    [Parameter(Mandatory = $true)][string]$Kind)

                if ($Kind -ceq 'package') {
                    Assert-PackageWiring $Ast
                    return
                }

                Assert-TemplateWiring $Ast
            }

            function Replace-AstExtent {
                param(
                    [Parameter(Mandatory = $true)][string]$Source,
                    [Parameter(Mandatory = $true)]$Node,
                    [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Replacement)

                return $Source.Substring(0, $Node.Extent.StartOffset) +
                    $Replacement +
                    $Source.Substring($Node.Extent.EndOffset)
            }

            function Swap-AstExtents {
                param(
                    [Parameter(Mandatory = $true)][string]$Source,
                    [Parameter(Mandatory = $true)]$First,
                    [Parameter(Mandatory = $true)]$Second)

                if ($First.Extent.EndOffset -gt $Second.Extent.StartOffset) {
                    throw 'AST swap requires ordered non-overlapping nodes'
                }

                return $Source.Substring(0, $First.Extent.StartOffset) +
                    $Second.Extent.Text +
                    $Source.Substring(
                        $First.Extent.EndOffset,
                        $Second.Extent.StartOffset - $First.Extent.EndOffset) +
                    $First.Extent.Text +
                    $Source.Substring($Second.Extent.EndOffset)
            }

            function Move-AstAfter {
                param(
                    [Parameter(Mandatory = $true)][string]$Source,
                    [Parameter(Mandatory = $true)]$Node,
                    [Parameter(Mandatory = $true)]$Container)

                if ($Node.Extent.StartOffset -lt $Container.Extent.StartOffset -or
                    $Node.Extent.EndOffset -gt $Container.Extent.EndOffset) {
                    throw 'AST move requires the node to be inside its container'
                }

                $nodeLength = $Node.Extent.EndOffset - $Node.Extent.StartOffset
                $sourceWithoutNode = Replace-AstExtent $Source $Node ''
                $insertOffset = $Container.Extent.EndOffset - $nodeLength
                return $sourceWithoutNode.Substring(0, $insertOffset) +
                    [Environment]::NewLine +
                    '    ' +
                    $Node.Extent.Text +
                    $sourceWithoutNode.Substring($insertOffset)
            }

            function Get-PackageMutations {
                return @(
                    @{
                        Name = 'package-source-pack-removed'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $command = @(Get-ScriptCommands $ast 'Restore-LockedAndPack')[0]
                            Replace-AstExtent $source $command ''
                        }
                    },
                    @{
                        Name = 'package-restore-removed'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $commands = @(Get-ScriptCommands $ast 'Restore-LockedAndBuild')
                            Replace-AstExtent $source $commands[0] ''
                        }
                    },
                    @{
                        Name = 'package-cache-argument-wrong'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $command = @(Get-ScriptCommands $ast 'Restore-LockedAndBuild')[0]
                            $argument = Get-CommandParameterArgument $command 'GlobalPackagesFolder'
                            Replace-AstExtent $source $argument '$wrongGlobalPackagesFolder'
                        }
                    },
                    @{
                        Name = 'package-project-argument-wrong'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $command = @(Get-ScriptCommands $ast 'Restore-LockedAndBuild')[0]
                            $argument = Get-CommandParameterArgument $command 'Project'
                            Replace-AstExtent $source $argument '$wrongConsumerProject'
                        }
                    },
                    @{
                        Name = 'package-nuget-config-argument-wrong'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $command = @(Get-ScriptCommands $ast 'Restore-LockedAndBuild')[0]
                            $argument = Get-CommandParameterArgument $command 'NuGetConfig'
                            Replace-AstExtent $source $argument '$wrongConsumerNuGetConfig'
                        }
                    },
                    @{
                        Name = 'package-restore-outside-consumer-loop'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $command = @(Get-ScriptCommands $ast 'Restore-LockedAndBuild')[0]
                            $consumerLoop = $command.Parent
                            while ($null -ne $consumerLoop -and
                                $consumerLoop -isnot [System.Management.Automation.Language.ForEachStatementAst]) {
                                $consumerLoop = $consumerLoop.Parent
                            }

                            if ($null -eq $consumerLoop) {
                                throw 'package restore mutation could not find its consumer loop'
                            }

                            Move-AstAfter $source $command $consumerLoop
                        }
                    },
                    @{
                        Name = 'package-cleanup-removed'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $command = @(Get-ScriptCommands $ast 'Remove-ControlledChild')[0]
                            Replace-AstExtent $source $command ''
                        }
                    }
                )
            }

            function Get-TemplateMutations {
                return @(
                    @{
                        Name = 'template-service-cache-argument-wrong'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $restores = @(Get-ScriptCommands $ast 'Restore-LockedAndBuild')
                            $command = Get-RestoreByProjectVariable $restores 'serviceRoot'
                            $argument = Get-CommandParameterArgument $command 'GlobalPackagesFolder'
                            Replace-AstExtent $source $argument '$wrongGlobalPackagesFolder'
                        }
                    },
                    @{
                        Name = 'template-gateway-cache-argument-wrong'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $restores = @(Get-ScriptCommands $ast 'Restore-LockedAndBuild')
                            $command = Get-RestoreByProjectVariable $restores 'gatewayRoot'
                            $argument = Get-CommandParameterArgument $command 'GlobalPackagesFolder'
                            Replace-AstExtent $source $argument '$wrongGlobalPackagesFolder'
                        }
                    },
                    @{
                        Name = 'template-building-cache-argument-wrong'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $restores = @(Get-ScriptCommands $ast 'Restore-LockedAndBuild')
                            $command = Get-RestoreByProjectVariable $restores 'testProject'
                            $argument = Get-CommandParameterArgument $command 'GlobalPackagesFolder'
                            Replace-AstExtent $source $argument '$globalPackagesFolder'
                        }
                    },
                    @{
                        Name = 'template-charter-validator-removed'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $command = @(Get-ScriptCommands $ast 'Invoke-CharterValidator')[0]
                            Replace-AstExtent $source $command ''
                        }
                    },
                    @{
                        Name = 'template-charter-validator-after-build'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $validator = @(Get-ScriptCommands $ast 'Invoke-CharterValidator')[0]
                            $restores = @(Get-ScriptCommands $ast 'Restore-LockedAndBuild')
                            $buildingRestore = Get-RestoreByProjectVariable $restores 'testProject'
                            Swap-AstExtents $source $validator $buildingRestore
                        }
                    },
                    @{
                        Name = 'template-building-cleanup-removed'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $command = @(Get-ScriptCommands $ast 'Complete-TemplateRun')[0]
                            Replace-AstExtent $source $command '$null'
                        }
                    },
                    @{
                        Name = 'template-temporary-cleanup-child-wrong'
                        Apply = {
                            param($source)
                            $ast = Parse-WiringAst $source
                            $command = @(Get-ScriptCommands $ast 'Complete-TemplateRun')[0]
                            $argument = Get-CommandParameterArgument $command 'RunRoot'
                            Replace-AstExtent $source $argument '$buildingSmokeRoot'
                        }
                    }
                )
            }

            function Assert-MutationsRejected {
                param(
                    [Parameter(Mandatory = $true)][string]$Source,
                    [Parameter(Mandatory = $true)][string]$Kind)

                $mutations = if ($Kind -ceq 'package') {
                    @(Get-PackageMutations)
                }
                else {
                    @(Get-TemplateMutations)
                }

                foreach ($mutation in $mutations) {
                    $mutatedSource = & $mutation.Apply $Source
                    if ($mutatedSource -ceq $Source) {
                        throw "mutation did not change source: $($mutation.Name)"
                    }

                    $mutatedAst = Parse-WiringAst $mutatedSource
                    $rejected = $false
                    try {
                        Assert-Wiring $mutatedAst $Kind
                    }
                    catch {
                        $rejected = $true
                    }

                    if (-not $rejected) {
                        throw "wiring validator accepted mutation: $($mutation.Name)"
                    }
                }
            }

            $scriptPath = {{PowerShellLiteral(scriptPath)}}
            $wiringKind = {{PowerShellLiteral(wiringKind)}}
            $source = [System.IO.File]::ReadAllText($scriptPath)
            $ast = Parse-WiringAst $source
            Assert-Wiring $ast $wiringKind
            if ({{verifyMutationsLiteral}}) {
                Assert-MutationsRejected $source $wiringKind
            }
            """;
    }

    /// <summary>
    /// 从生产脚本 AST 提取指定函数并生成独立执行 harness
    /// </summary>
    /// <param name="scriptPath">生产 PowerShell 脚本路径</param>
    /// <param name="functionNames">必须提取的生产函数名称</param>
    /// <param name="body">函数加载后执行的行为验证脚本</param>
    /// <returns>只包含真实生产函数与行为验证主体的 PowerShell 脚本</returns>
    private static string CreatePowerShellHarness(
        string scriptPath,
        IReadOnlyCollection<string> functionNames,
        string body)
    {
        var functionLiterals = string.Join(
            ", ",
            functionNames.Select(PowerShellLiteral));
        return $$"""
            $ErrorActionPreference = 'Stop'
            $scriptPath = {{PowerShellLiteral(scriptPath)}}
            $tokens = $null
            $parseErrors = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseFile(
                $scriptPath,
                [ref]$tokens,
                [ref]$parseErrors)
            if ($parseErrors.Count -ne 0) {
                throw "production script has parse errors: $($parseErrors -join '; ')"
            }

            foreach ($functionName in @({{functionLiterals}})) {
                $definitions = @($ast.FindAll({
                    param($node)
                    $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
                        $node.Name -ceq $functionName
                }, $true))
                if ($definitions.Count -ne 1) {
                    throw "expected exactly one production function $functionName; found $($definitions.Count)"
                }

                Invoke-Expression $definitions[0].Extent.Text
            }

            {{body}}
            """;
    }

    /// <summary>
    /// 执行临时 PowerShell harness 并捕获退出状态
    /// </summary>
    /// <param name="workingDirectory">harness 工作目录</param>
    /// <param name="harness">待执行的 PowerShell 脚本</param>
    /// <param name="environment">覆盖到子进程的环境变量</param>
    /// <returns>PowerShell 进程退出码和合并输出</returns>
    private static ProcessExecutionResult RunPowerShell(
        string workingDirectory,
        string harness,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var executable = OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh";
        var harnessPath = Path.Combine(workingDirectory, $"harness-{Guid.NewGuid():N}.ps1");
        File.WriteAllText(
            harnessPath,
            harness,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        var startInfo = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        if (OperatingSystem.IsWindows())
        {
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
        }

        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(harnessPath);
        if (environment is not null)
        {
            foreach (var variable in environment)
            {
                if (variable.Value is null)
                {
                    startInfo.Environment.Remove(variable.Key);
                }
                else
                {
                    startInfo.Environment[variable.Key] = variable.Value;
                }
            }
        }

        try
        {
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("无法启动 PowerShell 行为测试 harness");
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return new ProcessExecutionResult(
                process.ExitCode,
                string.Join(Environment.NewLine, standardOutput, standardError));
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            Assert.Skip($"当前平台没有可用的 PowerShell：{exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// 创建记录环境变量并返回指定退出码的 dotnet 替身进程
    /// </summary>
    /// <param name="directory">替身命令目录</param>
    /// <param name="exitCode">每次调用返回的退出码</param>
    private static void CreateFakeDotNet(string directory, int exitCode)
    {
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(
                Path.Combine(directory, "dotnet.cmd"),
                $"@echo %NUGET_PACKAGES%>>\"%TW_CAPTURE_PATH%\"{Environment.NewLine}@exit /b {exitCode}{Environment.NewLine}");
            return;
        }

        var executablePath = Path.Combine(directory, "dotnet");
        File.WriteAllText(
            executablePath,
            $"#!/bin/sh{Environment.NewLine}printf '%s\\n' \"$NUGET_PACKAGES\" >> \"$TW_CAPTURE_PATH\"{Environment.NewLine}exit {exitCode}{Environment.NewLine}");
        File.SetUnixFileMode(
            executablePath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    /// <summary>
    /// 创建同时记录 NUGET_PACKAGES 与完整参数的成功 dotnet 替身进程
    /// </summary>
    /// <param name="directory">替身命令目录</param>
    private static void CreateArgumentCapturingFakeDotNet(string directory)
    {
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(
                Path.Combine(directory, "dotnet.cmd"),
                $"@echo %NUGET_PACKAGES%^|%*>>\"%TW_CAPTURE_PATH%\"{Environment.NewLine}@exit /b 0{Environment.NewLine}");
            return;
        }

        var executablePath = Path.Combine(directory, "dotnet");
        File.WriteAllText(
            executablePath,
            $"#!/bin/sh{Environment.NewLine}printf '%s|%s\\n' \"$NUGET_PACKAGES\" \"$*\" >> \"$TW_CAPTURE_PATH\"{Environment.NewLine}exit 0{Environment.NewLine}");
        File.SetUnixFileMode(
            executablePath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    /// <summary>
    /// 创建记录完整参数并返回指定失败码的 dotnet 替身进程
    /// </summary>
    /// <param name="directory">替身命令目录</param>
    /// <param name="exitCode">替身返回码</param>
    private static void CreateArgumentCapturingFailingFakeDotNet(string directory, int exitCode)
    {
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(
                Path.Combine(directory, "dotnet.cmd"),
                $"@echo %*>>\"%TW_CAPTURE_PATH%\"{Environment.NewLine}@exit /b {exitCode}{Environment.NewLine}");
            return;
        }

        var executablePath = Path.Combine(directory, "dotnet");
        File.WriteAllText(
            executablePath,
            $"#!/bin/sh{Environment.NewLine}printf '%s\\n' \"$*\" >> \"$TW_CAPTURE_PATH\"{Environment.NewLine}exit {exitCode}{Environment.NewLine}");
        File.SetUnixFileMode(
            executablePath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    /// <summary>
    /// 创建记录完整参数并返回指定退出码的 python3 替身进程
    /// </summary>
    /// <param name="directory">替身命令目录</param>
    /// <param name="exitCode">替身返回码</param>
    private static void CreateFakePython3(string directory, int exitCode)
    {
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(
                Path.Combine(directory, "python3.cmd"),
                $"@echo %*>>\"%TW_CAPTURE_PATH%\"{Environment.NewLine}@exit /b {exitCode}{Environment.NewLine}");
            return;
        }

        var executablePath = Path.Combine(directory, "python3");
        File.WriteAllText(
            executablePath,
            $"#!/bin/sh{Environment.NewLine}printf '%s\\n' \"$*\" >> \"$TW_CAPTURE_PATH\"{Environment.NewLine}exit {exitCode}{Environment.NewLine}");
        File.SetUnixFileMode(
            executablePath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    /// <summary>
    /// 创建在指定子命令返回前把 feed 替换为外部 reparse point 的 dotnet 替身
    /// </summary>
    /// <param name="directory">替身命令目录</param>
    private static void CreateFeedSwappingFakeDotNet(string directory)
    {
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(
                Path.Combine(directory, "dotnet.cmd"),
                "@echo %1>>\"%TW_CAPTURE_PATH%\"\r\n" +
                "@if /I \"%1\"==\"%TW_SWAP_STAGE%\" (\r\n" +
                "  @rmdir \"%TW_FEED_PATH%\"\r\n" +
                "  @mklink /J \"%TW_FEED_PATH%\" \"%TW_EXTERNAL_PATH%\" >nul\r\n" +
                ")\r\n" +
                "@exit /b 0\r\n");
            return;
        }

        var executablePath = Path.Combine(directory, "dotnet");
        File.WriteAllText(
            executablePath,
            "#!/bin/sh\n" +
            "printf '%s\\n' \"$1\" >> \"$TW_CAPTURE_PATH\"\n" +
            "if [ \"$1\" = \"$TW_SWAP_STAGE\" ]; then\n" +
            "  rmdir \"$TW_FEED_PATH\"\n" +
            "  ln -s \"$TW_EXTERNAL_PATH\" \"$TW_FEED_PATH\"\n" +
            "fi\n" +
            "exit 0\n");
        File.SetUnixFileMode(
            executablePath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    /// <summary>
    /// 创建在指定调用序号返回前把 feed 替换为外部 reparse point 的 dotnet 替身
    /// </summary>
    /// <param name="directory">替身命令目录</param>
    private static void CreateInvocationFeedSwappingFakeDotNet(string directory)
    {
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(
                Path.Combine(directory, "dotnet.cmd"),
                "@setlocal EnableDelayedExpansion\r\n" +
                "@set count=0\r\n" +
                "@if exist \"%TW_COUNTER_PATH%\" set /p count=<\"%TW_COUNTER_PATH%\"\r\n" +
                "@set /a count+=1\r\n" +
                "@echo !count!>\"%TW_COUNTER_PATH%\"\r\n" +
                "@echo %1>>\"%TW_CAPTURE_PATH%\"\r\n" +
                "@if \"!count!\"==\"%TW_SWAP_INVOCATION%\" (\r\n" +
                "  @rmdir \"%TW_FEED_PATH%\"\r\n" +
                "  @mklink /J \"%TW_FEED_PATH%\" \"%TW_EXTERNAL_PATH%\" >nul\r\n" +
                ")\r\n" +
                "@exit /b 0\r\n");
            return;
        }

        var executablePath = Path.Combine(directory, "dotnet");
        File.WriteAllText(
            executablePath,
            "#!/bin/sh\n" +
            "count=0\n" +
            "if [ -f \"$TW_COUNTER_PATH\" ]; then count=$(cat \"$TW_COUNTER_PATH\"); fi\n" +
            "count=$((count + 1))\n" +
            "printf '%s\\n' \"$count\" > \"$TW_COUNTER_PATH\"\n" +
            "printf '%s\\n' \"$1\" >> \"$TW_CAPTURE_PATH\"\n" +
            "if [ \"$count\" = \"$TW_SWAP_INVOCATION\" ]; then\n" +
            "  rmdir \"$TW_FEED_PATH\"\n" +
            "  ln -s \"$TW_EXTERNAL_PATH\" \"$TW_FEED_PATH\"\n" +
            "fi\n" +
            "exit 0\n");
        File.SetUnixFileMode(
            executablePath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    /// <summary>
    /// 将目录置于当前 PATH 首位
    /// </summary>
    /// <param name="directory">优先解析命令的目录</param>
    /// <returns>包含原 PATH 的新值</returns>
    private static string PrependPath(string directory)
    {
        return string.Join(
            Path.PathSeparator,
            directory,
            Environment.GetEnvironmentVariable("PATH") ?? string.Empty);
    }

    /// <summary>
    /// 将路径编码为单引号 PowerShell 字面量
    /// </summary>
    /// <param name="value">待编码文本</param>
    /// <returns>不会插值的 PowerShell 字面量</returns>
    private static string PowerShellLiteral(string value)
    {
        return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
    }

    /// <summary>
    /// 创建指向外部目录的 junction 或符号链接
    /// </summary>
    /// <param name="linkPath">reparse point 路径</param>
    /// <param name="targetPath">外部目标目录</param>
    /// <returns>成功时返回 null，平台不支持时返回诊断文本</returns>
    private static string? TryCreateDirectoryReparsePoint(string linkPath, string targetPath)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                Directory.CreateSymbolicLink(linkPath, targetPath);
                return null;
            }

            var startInfo = new ProcessStartInfo("cmd.exe")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("/d");
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add("mklink");
            startInfo.ArgumentList.Add("/J");
            startInfo.ArgumentList.Add(linkPath);
            startInfo.ArgumentList.Add(targetPath);
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("无法启动 junction 创建命令");
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0
                ? null
                : string.Join(Environment.NewLine, standardOutput, standardError);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or System.ComponentModel.Win32Exception)
        {
            return exception.Message;
        }
    }

    /// <summary>
    /// 仅移除测试创建的目录 reparse point，不遍历其目标
    /// </summary>
    /// <param name="path">测试创建的 junction 或符号链接</param>
    private static void DeleteDirectoryReparsePoint(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReparsePoint) == 0)
        {
            throw new InvalidOperationException($"拒绝把普通目录作为测试 reparse point 删除：{path}");
        }

        Directory.Delete(path, recursive: false);
    }

    /// <summary>
    /// 表示 MSBuild 评估后的包引用
    /// </summary>
    /// <param name="PackageId">包标识</param>
    /// <param name="Version">显式包版本；中央管理时为空</param>
    private sealed record EvaluatedPackageReference(string PackageId, string? Version);

    /// <summary>
    /// 表示已完成进程的退出状态和诊断输出
    /// </summary>
    /// <param name="ExitCode">进程退出码</param>
    /// <param name="CombinedOutput">标准输出与标准错误的合并文本</param>
    private sealed record ProcessExecutionResult(int ExitCode, string CombinedOutput);

    /// <summary>
    /// 表示 MSBuild 评估后的仓库品牌 analyzer 接线
    /// </summary>
    /// <param name="Enabled">是否启用仓库品牌 analyzer</param>
    /// <param name="Twgov001IsError">TWGOV001 是否被提升为错误</param>
    /// <param name="AnalyzerReferences">自动 analyzer 项目引用完整路径</param>
    private sealed record AnalyzerWiringEvaluation(
        bool Enabled,
        bool Twgov001IsError,
        string[] AnalyzerReferences);

    /// <summary>
    /// 将项目文件路径转换为稳定的跨平台比较格式
    /// </summary>
    /// <param name="path">项目文件中的 Include 路径</param>
    /// <returns>使用正斜杠分隔的项目路径</returns>
    private static string NormalizeProjectPath(string? path)
    {
        return path?.Replace('\\', '/') ?? string.Empty;
    }

    /// <summary>
    /// 判断内部包引用是否只在项目引用兜底未启用时参与还原
    /// </summary>
    /// <param name="reference">内部包引用元素</param>
    /// <returns>包引用所在 ItemGroup 是否具备兜底条件</returns>
    private static bool UsesPackageFallbackCondition(XElement reference)
    {
        return reference
            .Ancestors("ItemGroup")
            .Any(group => group.Attribute("Condition")?.Value.Contains(
                "'$(UseRepositoryProjectReferences)' != 'true'",
                StringComparison.Ordinal) == true);
    }
}
