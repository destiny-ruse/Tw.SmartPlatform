using AwesomeAssertions;
using Tw.TextTemplating.Scriban;
using Xunit;

namespace Tw.TextTemplating.Scriban.Tests;

public sealed class TemplateFileAccessPolicyTests
{
    [Fact]
    public void Validate_RejectsPathOutsideRegisteredRoot()
    {
        var policy = new TemplateFileAccessPolicy(["D:/app/templates"]);

        var act = () => policy.Validate("D:/app/secrets/key.sbn");

        act.Should().Throw<TemplateFileAccessException>()
            .WithMessage("模板文件只能从注册的模板根目录读取");
    }
}
