using AwesomeAssertions;
using Tw.Configuration.Json;
using Xunit;

namespace Tw.Configuration.Json.Tests;

/// <summary>
/// 覆盖JSONConfiguration路径Validator的核心行为和边界条件
/// </summary>
public sealed class JsonConfigurationPathValidatorTests
{
    /// <summary>
    /// 验证校验拒绝路径OutsideAllowedRoots
    /// </summary>
    [Fact]
    public void Validate_RejectsPathOutsideAllowedRoots()
    {
        var validator = new JsonConfigurationPathValidator(
            contentRoot: "D:/app",
            allowedRoots: ["D:/app/config"]);

        var act = () => validator.Validate("D:/secrets/appsettings.json");

        act.Should().Throw<ConfigurationPathException>()
            .WithMessage("*outside allowed configuration roots*");
    }
}
