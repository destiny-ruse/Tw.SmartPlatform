using FluentAssertions;
using Microsoft.Extensions.Localization;
using Tw.Localization.Json;
using Xunit;

namespace Tw.Localization.AspNetCore.Tests;

public class TwStringLocalizerTests
{
    [Fact]
    public void Indexer_ReturnsStaticSnapshotText()
    {
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource("App", "zh-Hans", new Dictionary<string, string> { ["Menu"] = "菜单" })]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new TwStringLocalizer(snapshot, accessor, options, "App");

        var value = localizer["Menu"];

        value.Value.Should().Be("菜单");
        value.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_ReturnsKeyForMissingText()
    {
        var snapshot = new StaticTextSnapshot([]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new TwStringLocalizer(snapshot, accessor, options, "App");

        var value = localizer["Missing"];

        value.Value.Should().Be("Missing");
        value.ResourceNotFound.Should().BeTrue();
    }
}
