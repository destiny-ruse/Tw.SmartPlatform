using FluentAssertions;
using Tw.Localization.Json;
using Tw.Localization.Requests;
using Xunit;

namespace Tw.Localization.Tests;

public class JsonTextResourceContributorTests
{
    [Fact]
    public async Task GetOrNullAsync_ReturnsCurrentCultureText()
    {
        var resource = new JsonTextResource("App", "zh-Hans", new Dictionary<string, string> { ["Menu"] = "菜单" });
        var contributor = new JsonTextResourceContributor([resource], priority: 0);
        var request = new TextLookupRequest("App", "Menu", new LocalizationContext("zh-Hans"), ["zh-Hans"]);

        var text = await contributor.GetOrNullAsync(request);

        text!.Value.Should().Be("菜单");
        text.Source.Should().Be(LocalizedTextSource.StaticJson);
    }

    [Fact]
    public void StaticSnapshot_ReturnsFallbackCultureText()
    {
        var resources = new[]
        {
            new JsonTextResource("App", "en-US", new Dictionary<string, string> { ["Menu"] = "Menu" }),
        };
        var snapshot = new StaticTextSnapshot(resources);

        var text = snapshot.Find("App", "Menu", ["zh-Hans", "en-US"]);

        text!.Value.Should().Be("Menu");
    }
}
