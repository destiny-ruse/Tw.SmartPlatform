using FluentAssertions;
using Tw.Localization;
using Xunit;

namespace Tw.Localization.AspNetCore.Tests;

public class RequestCultureResolverTests
{
    private static LocalizationOptions Options()
    {
        return new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "en-US", "zh-Hans" },
        };
    }

    [Fact]
    public void Resolve_UsesRouteBeforeQuery()
    {
        var result = RequestCultureResolver.Resolve(
            routeCulture: "zh-Hans",
            queryCulture: "en-US",
            cookieCulture: null,
            acceptLanguageHeader: null,
            Options());

        result.CultureName.Should().Be("zh-Hans");
        result.IsExplicitSwitch.Should().BeTrue();
    }

    [Fact]
    public void Resolve_UsesDefaultForUnsupportedCulture()
    {
        var result = RequestCultureResolver.Resolve(
            routeCulture: null,
            queryCulture: "fr-FR",
            cookieCulture: null,
            acceptLanguageHeader: null,
            Options());

        result.CultureName.Should().Be("en-US");
    }
}
