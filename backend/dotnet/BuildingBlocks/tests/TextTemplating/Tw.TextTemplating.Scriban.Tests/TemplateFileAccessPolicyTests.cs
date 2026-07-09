using AwesomeAssertions;
using Tw.TextTemplating.Scriban;
using Xunit;

namespace Tw.TextTemplating.Scriban.Tests;

/// <summary>验证 TemplateFileAccessPolicyTests 相关行为</summary>
public sealed class TemplateFileAccessPolicyTests
{
    /// <summary>验证 Validate_RejectsPathOutsideRegisteredRoot 场景</summary>
    [Fact]
    public void Validate_RejectsPathOutsideRegisteredRoot()
    {
        var policy = new TemplateFileAccessPolicy(["D:/app/templates"]);

        var act = () => policy.Validate("D:/app/secrets/key.sbn");

        act.Should().Throw<TemplateFileAccessException>()
            .WithMessage("模板文件只能从注册的模板根目录读取");
    }
}
