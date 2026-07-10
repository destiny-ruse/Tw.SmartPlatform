using AwesomeAssertions;
using Tw.TextTemplating.Scriban;
using Xunit;

namespace Tw.TextTemplating.Scriban.Tests;

/// <summary>
/// 覆盖模板FileAccess策略的核心行为和边界条件
/// </summary>
public sealed class TemplateFileAccessPolicyTests
{
    /// <summary>
    /// 验证校验拒绝路径OutsideRegistered根目录
    /// </summary>
    [Fact]
    public void Validate_RejectsPathOutsideRegisteredRoot()
    {
        var policy = new TemplateFileAccessPolicy(["D:/app/templates"]);

        var act = () => policy.Validate("D:/app/secrets/key.sbn");

        act.Should().Throw<TemplateFileAccessException>()
            .WithMessage("模板文件只能从注册的模板根目录读取");
    }
}
