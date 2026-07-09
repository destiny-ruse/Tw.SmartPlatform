using AwesomeAssertions;
using Tw.Exceptions;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>验证 LocalizationOptionsTests 相关行为</summary>
public class LocalizationOptionsTests
{
    /// <summary>验证 Validate_RejectsInvalidDefaultCulture 场景</summary>
    [Fact]
    public void Validate_RejectsInvalidDefaultCulture()
    {
        var options = new LocalizationOptions { DefaultCulture = "not a culture" };

        var act = () => options.Validate();

        act.Should().Throw<TwConfigurationException>();
    }

    /// <summary>验证 Validate_RequiresDefaultCultureInSupportedList 场景</summary>
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

    /// <summary>验证 Validate_PassesForValidConfig 场景</summary>
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
