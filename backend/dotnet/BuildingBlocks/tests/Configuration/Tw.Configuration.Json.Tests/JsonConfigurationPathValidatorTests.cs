using AwesomeAssertions;
using Tw.Configuration.Json;
using Xunit;

namespace Tw.Configuration.Json.Tests;

/// <summary>验证 JsonConfigurationPathValidatorTests 相关行为</summary>
public sealed class JsonConfigurationPathValidatorTests
{
    /// <summary>验证 Validate_RejectsPathOutsideAllowedRoots 场景</summary>
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
