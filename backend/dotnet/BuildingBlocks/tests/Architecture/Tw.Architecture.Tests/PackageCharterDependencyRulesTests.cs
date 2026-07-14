using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 使用临时章程固定应用包收敛门禁的 YAML 解析边界
/// </summary>
public sealed partial class PackageConsolidationTests
{
    /// <summary>
    /// 包含 Tw.Core 的有效 YAML 白名单写法
    /// </summary>
    public static TheoryData<string> CharterAllowingTwCoreCases => new()
    {
        {
            """
            dependency_rules:
              allow:
                - Tw.Core
            """
        },
        {
            """
            dependency_rules: { allow: [Other.Package, tw.CORE] }
            """
        },
        {
            """
            dependency_rules:
                  allow:

                      # 保留大小写不同的依赖身份
                      - TW.core
            """
        }
    };

    /// <summary>
    /// 不允许 Tw.Core 的有效 YAML 章程写法
    /// </summary>
    public static TheoryData<string> CharterNotAllowingTwCoreCases => new()
    {
        {
            """
            dependency_rules:
              forbid: [Tw.Core]
            """
        },
        {
            """
            other_field:
              allow: [Tw.Core]
            dependency_rules:
              allow: [Other.Package]
            """
        },
        {
            """
            responsibility: Tw.Core
            dependency_rules:
              allow: [Tw.Core.Extensions]
            """
        },
        {
            """
            package: Example.Package
            """
        }
    };

    /// <summary>
    /// 必须产生明确诊断的非法 YAML 或无效章程节点结构
    /// </summary>
    public static TheoryData<string> InvalidCharterCases => new()
    {
        { "dependency_rules: { allow: [Tw.Core }" },
        {
            """
            dependency_rules:
              allow:
                - Tw.Core
                - nested: invalid
            """
        }
    };

    /// <summary>
    /// 有效 YAML 的 block、inline、缩进、注释与大小写变化均能识别 Tw.Core
    /// </summary>
    /// <param name="yaml">写入临时章程的 YAML 文本</param>
    [Theory]
    [MemberData(nameof(CharterAllowingTwCoreCases))]
    public void CharterAllowsTwCore_ReturnsTrue_ForSupportedYamlForms(string yaml)
    {
        using var directory = new TemporaryTestDirectory();
        var charterPath = directory.WriteFile("package-charter.yaml", yaml);

        CharterAllowsTwCore(charterPath).Should().BeTrue();
    }

    /// <summary>
    /// Tw.Core 出现在 forbid、其他字段、相似值或缺失 dependency_rules 时不视为允许依赖
    /// </summary>
    /// <param name="yaml">写入临时章程的 YAML 文本</param>
    [Theory]
    [MemberData(nameof(CharterNotAllowingTwCoreCases))]
    public void CharterAllowsTwCore_ReturnsFalse_WhenAllowDoesNotContainTwCore(string yaml)
    {
        using var directory = new TemporaryTestDirectory();
        var charterPath = directory.WriteFile("package-charter.yaml", yaml);

        CharterAllowsTwCore(charterPath).Should().BeFalse();
    }

    /// <summary>
    /// 非法 YAML 与错误节点结构必须携带文件诊断失败，不能被解释为未允许 Tw.Core
    /// </summary>
    /// <param name="yaml">写入临时章程的无效 YAML 文本</param>
    [Theory]
    [MemberData(nameof(InvalidCharterCases))]
    public void CharterAllowsTwCore_ThrowsDiagnosticFailure_WhenYamlIsInvalid(string yaml)
    {
        using var directory = new TemporaryTestDirectory();
        var charterPath = directory.WriteFile("invalid-package-charter.yaml", yaml);

        Action act = () => CharterAllowsTwCore(charterPath);

        act.Should()
            .Throw<InvalidDataException>()
            .WithMessage($"*{Path.GetFileName(charterPath)}*");
    }

    /// <summary>
    /// 零依赖门禁要求章程显式声明空 allow，不能把缺失节点解释为空列表
    /// </summary>
    [Fact]
    public void ReadAllowedDependencies_ThrowsDiagnosticFailure_WhenAllowNodeIsMissing()
    {
        using var directory = new TemporaryTestDirectory();
        var charterPath = directory.WriteFile(
            "package-charter.yaml",
            """
            dependency_rules:
              forbid: [Polly*]
            """);

        Action act = () => PackageCharterDependencyRules.ReadAllowedDependencies(charterPath);

        act.Should()
            .Throw<InvalidDataException>()
            .WithMessage("*dependency_rules.allow 必须存在*");
    }
}
