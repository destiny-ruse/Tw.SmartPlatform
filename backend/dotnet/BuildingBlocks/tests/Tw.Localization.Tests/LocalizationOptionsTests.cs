using AwesomeAssertions;
using Tw.Exceptions;
using Xunit;

namespace Tw.Localization.Tests;

public class LocalizationOptionsTests
{
    [Fact]
    public void Validate_RejectsInvalidDefaultCulture()
    {
        var options = new LocalizationOptions { DefaultCulture = "not a culture" };

        var act = () => options.Validate();

        act.Should().Throw<TwConfigurationException>();
    }

    [Fact]
    public void Validate_RequiresDefaultCultureInSupportedList()
    {
        var options = new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "zh-Hans" },
        };

        var act = () => options.Validate();

        act.Should().Throw<TwConfigurationException>();
    }

    [Fact]
    public void Validate_PassesForValidConfig()
    {
        var options = new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "en-US", "zh-Hans" },
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }
}
