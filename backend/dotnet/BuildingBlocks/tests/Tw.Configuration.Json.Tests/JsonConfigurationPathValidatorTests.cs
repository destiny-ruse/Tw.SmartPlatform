using AwesomeAssertions;
using Tw.Configuration.Json;
using Xunit;

namespace Tw.Configuration.Json.Tests;

public sealed class JsonConfigurationPathValidatorTests
{
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
