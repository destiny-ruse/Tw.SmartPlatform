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
    public static TheoryData<string> ProjectReferencingTwCoreCases => new()
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
    [MemberData(nameof(ProjectReferencingTwCoreCases))]
    public void ReferencesTwCore_ReturnsTrue_ForStaticScannerInputForms(string projectXml)
    {
        using var directory = new TemporaryTestDirectory();
        var projectPath = directory.WriteFile("Consumer.csproj", projectXml);

        ReferencesTwCore(projectPath).Should().BeTrue();
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
}
