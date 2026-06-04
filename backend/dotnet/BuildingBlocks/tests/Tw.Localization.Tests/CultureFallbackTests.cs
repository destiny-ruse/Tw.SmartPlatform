using FluentAssertions;
using Tw.Exceptions;
using Xunit;

namespace Tw.Localization.Tests;

public class CultureFallbackTests
{
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
}
