using AwesomeAssertions;
using Tw.Exceptions;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>验证 CultureFallbackTests 相关行为</summary>
public class CultureFallbackTests
{
    /// <summary>验证 Expand_ReturnsCurrentParentAndDefault 场景</summary>
    [Fact]
    public void Expand_ReturnsCurrentParentAndDefault()
    {
        var options = new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "en-US", "zh", "zh-Hans" },
        };
        var context = new LocalizationContext("zh-Hans");

        CultureFallback.Expand(context, options).Should().Equal("zh-Hans", "zh", "en-US");
    }

    /// <summary>验证 Expand_DoesNotDuplicateDefault 场景</summary>
    [Fact]
    public void Expand_DoesNotDuplicateDefault()
    {
        var options = new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "en-US" },
        };
        var context = new LocalizationContext("en-US");

        CultureFallback.Expand(context, options).Should().Equal("en-US");
    }

    /// <summary>验证 Expand_SuppressesParentChainAndDefault_WhenFallbackDisabled 场景</summary>
    [Fact]
    public void Expand_SuppressesParentChainAndDefault_WhenFallbackDisabled()
    {
        var options = new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "en-US", "zh", "zh-Hans" },
            FallbackToParentCultures = false,
            FallbackToDefaultCulture = false,
        };
        var context = new LocalizationContext("zh-Hans");

        CultureFallback.Expand(context, options).Should().Equal("zh-Hans");
    }

    /// <summary>验证 Expand_DoesNotThrow_WhenCurrentCultureIsInvalid 场景</summary>
    [Fact]
    public void Expand_DoesNotThrow_WhenCurrentCultureIsInvalid()
    {
        var options = new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "en-US" },
        };
        var context = new LocalizationContext("not a culture");

        CultureFallback.Expand(context, options).Should().Equal("not a culture", "en-US");
    }
}
