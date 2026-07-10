using AwesomeAssertions;
using Tw.Exceptions;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 覆盖文化回退的核心行为和边界条件
/// </summary>
public class CultureFallbackTests
{
    /// <summary>
    /// 验证Expand返回Current父级和默认
    /// </summary>
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

    /// <summary>
    /// 验证Expand不重复默认
    /// </summary>
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

    /// <summary>
    /// 验证ExpandSuppresses父级Chain和默认当回退Disabled
    /// </summary>
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

    /// <summary>
    /// 验证Expand不Throw当Current文化Is非法
    /// </summary>
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
