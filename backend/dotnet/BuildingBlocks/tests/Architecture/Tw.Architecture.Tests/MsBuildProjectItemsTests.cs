using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 使用临时项目固定应用包收敛门禁的静态 MSBuild XML 解析边界
/// </summary>
public sealed partial class PackageConsolidationTests
{
    /// <summary>
    /// 用于验证静态扫描能力的 Tw.Core XML 输入，不声明具体 NuGet item-spec 的合法性
    /// </summary>
    public static TheoryData<string> ProjectReferencingCorePackageCases => new()
    {
        {
            """
            <Project>
              <ItemGroup>
                <pRoJeCtReFeReNcE Include="../Tw.Core/Tw.Core.csproj" />
              </ItemGroup>
            </Project>
            """
        },
        {
            """
            <Project xmlns="urn:example:msbuild">
              <ItemGroup>
                <ProjectReference Include="../Tw.Core/Tw.Core.csproj; ../Other/Other.csproj" />
              </ItemGroup>
            </Project>
            """
        },
        {
            """
            <Project>
              <ItemGroup>
                <PackageReference Include="Other.Package; tw.core" />
              </ItemGroup>
            </Project>
            """
        }
    };

    /// <summary>
    /// 不能按字面文件路径解析的 MSBuild 表达式 item-spec
    /// </summary>
    public static TheoryData<string> ProjectReferenceExpressionCases => new()
    {
        { "$(ProjectRoot)/Existing.csproj" },
        { "@(GeneratedProject)" },
        { "%(ProjectReference.Identity)" }
    };

    /// <summary>
    /// 元素大小写、XML namespace 与组合 Include 均不能绕过静态 Tw.Core 扫描
    /// </summary>
    /// <param name="projectXml">写入临时项目的 MSBuild XML 文本</param>
    [Theory]
    [MemberData(nameof(ProjectReferencingCorePackageCases))]
    public void ReferencesCorePackage_ReturnsTrue_ForStaticScannerInputForms(string projectXml)
    {
        using var directory = new TemporaryTestDirectory();
        var projectPath = directory.WriteFile("Consumer.csproj", projectXml);

        ReferencesCorePackage(projectPath).Should().BeTrue();
    }

    /// <summary>
    /// 带 namespace 的混合大小写 ProjectReference 会拆分组合 Include 并只报告缺失项目
    /// </summary>
    [Fact]
    public void FindUnresolvedProjectReferences_ResolvesCombinedNamespacedItems()
    {
        using var directory = new TemporaryTestDirectory();
        directory.WriteFile("Existing.csproj", "<Project />");
        var projectPath = directory.WriteFile(
            "Consumer.csproj",
            """
            <Project xmlns="urn:example:msbuild">
              <ItemGroup>
                <pRoJeCtReFeReNcE Include=" Existing.csproj ; Missing.csproj " />
              </ItemGroup>
            </Project>
            """);

        var violations = FindUnresolvedProjectReferences(projectPath).ToArray();

        violations.Should().Equal(
            $"{RepositoryLayout.RepositoryRelativePath(projectPath)} references missing project Missing.csproj");
    }

    /// <summary>
    /// 缺失 Include 或仅有相似属性名时保持明确诊断且不读取属性边界之外的值
    /// </summary>
    [Fact]
    public void FindUnresolvedProjectReferences_ReportsMissingIncludeAtExactAttributeBoundary()
    {
        using var directory = new TemporaryTestDirectory();
        var projectPath = directory.WriteFile(
            "Consumer.csproj",
            """
            <Project xmlns:custom="urn:custom:attributes">
              <ItemGroup>
                <ProjectReference />
                <ProjectReference NotInclude="Missing.csproj" />
                <ProjectReference custom:Include="Missing.csproj" />
              </ItemGroup>
            </Project>
            """);

        var violations = FindUnresolvedProjectReferences(projectPath).ToArray();

        violations.Should().HaveCount(3)
            .And.OnlyContain(message => message.EndsWith(
                "has a ProjectReference without Include",
                StringComparison.Ordinal));
    }

    /// <summary>
    /// ProjectReference 表达式必须按单项报告，不能因本地存在同名字面路径而被当作文件
    /// </summary>
    /// <param name="expression">需要 MSBuild evaluation 的 item-spec</param>
    [Theory]
    [MemberData(nameof(ProjectReferenceExpressionCases))]
    public void FindUnresolvedProjectReferences_ReportsExpressionBeforeFileResolution(string expression)
    {
        using var directory = new TemporaryTestDirectory();
        directory.WriteFile(expression, "<Project />");
        var projectPath = directory.WriteFile(
            "Consumer.csproj",
            $$"""
            <Project>
              <ItemGroup>
                <ProjectReference Include="{{expression}}" />
              </ItemGroup>
            </Project>
            """);

        var violations = FindUnresolvedProjectReferences(projectPath).ToArray();

        violations.Should().Equal(
            $"{RepositoryLayout.RepositoryRelativePath(projectPath)} uses an unresolved ProjectReference expression: {expression}");
    }

    /// <summary>
    /// Windows 与 Unix 风格分隔符都必须转换为目标宿主可解析的 item-spec
    /// </summary>
    /// <param name="itemSpec">包含混合分隔符的项目引用路径</param>
    /// <param name="directorySeparator">模拟目标宿主的目录分隔符</param>
    /// <param name="expected">目标宿主应接收的路径文本</param>
    [Theory]
    [InlineData("..\\Tw.Http.Client/Tw.Http.Client.csproj", '/', "../Tw.Http.Client/Tw.Http.Client.csproj")]
    [InlineData("../Tw.Http.Client\\Tw.Http.Client.csproj", '\\', "..\\Tw.Http.Client\\Tw.Http.Client.csproj")]
    public void NormalizeFileSystemPath_UsesTargetHostSeparator(
        string itemSpec,
        char directorySeparator,
        string expected)
    {
        MsBuildProjectItems.NormalizeFileSystemPath(itemSpec, directorySeparator).Should().Be(expected);
    }

    /// <summary>
    /// Windows 风格 ProjectReference 在任意宿主上均应解析到现存目标并提取规范项目名
    /// </summary>
    [Fact]
    public void WindowsProjectReference_ResolvesExistingTargetAndCanonicalProjectName()
    {
        using var directory = new TemporaryTestDirectory();
        directory.WriteFile("Referenced/Tw.Http.Client.csproj", "<Project />");
        var projectPath = directory.WriteFile(
            "Consumer.csproj",
            "<Project><ItemGroup><ProjectReference Include=\"Referenced\\Tw.Http.Client.csproj\" /></ItemGroup></Project>");

        var normalized = MsBuildProjectItems.NormalizeFileSystemPath(
            "Referenced\\Tw.Http.Client.csproj",
            Path.DirectorySeparatorChar);

        Path.GetFileNameWithoutExtension(normalized).Should().Be("Tw.Http.Client");
        FindUnresolvedProjectReferences(projectPath).Should().BeEmpty();
    }

    /// <summary>
    /// 根命名空间重复声明时合并门禁必须读取最后生效值
    /// </summary>
    [Fact]
    public void EffectiveRootNamespace_UsesLastExplicitValue()
    {
        using var directory = new TemporaryTestDirectory();
        var projectPath = directory.WriteFile(
            "Tw.Sample.csproj",
            "<Project><PropertyGroup><RootNamespace>Tw.Sample</RootNamespace><RootNamespace>Tw.Unapproved</RootNamespace></PropertyGroup></Project>");

        EffectiveRootNamespace(projectPath).Should().Be("Tw.Unapproved");
    }
}
