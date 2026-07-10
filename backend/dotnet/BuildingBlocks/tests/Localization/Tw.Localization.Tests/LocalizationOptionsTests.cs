using AwesomeAssertions;
using Tw.Exceptions;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 验证本地化选项的默认值和配置合法性校验
/// </summary>
public class LocalizationOptionsTests
{
    /// <summary>
    /// 验证本地化选项默认使用简体中文文化作为最终回退文化
    /// </summary>
    [Fact]
    public void Constructor_UsesSimplifiedChineseAsDefaultCulture()
    {
        var options = new LocalizationOptions();

        options.DefaultCulture.Should().Be("zh-Hans");
    }

    /// <summary>
    /// 验证默认文化不是合法 BCP 47 名称时配置校验失败
    /// </summary>
    [Fact]
    public void Validate_RejectsInvalidDefaultCulture()
    {
        var options = new LocalizationOptions { DefaultCulture = "not a culture" };

        var act = () => options.Validate();

        act.Should().Throw<TwConfigurationException>();
    }

    /// <summary>
    /// 验证默认文化未包含在支持文化列表中时配置校验失败
    /// </summary>
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

    /// <summary>
    /// 验证默认文化和支持文化列表合法时配置校验通过
    /// </summary>
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
