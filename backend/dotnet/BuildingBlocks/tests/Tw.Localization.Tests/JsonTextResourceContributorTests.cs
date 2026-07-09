using AwesomeAssertions;
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

    [Fact]
    public async Task FillAsync_HigherPriorityCultureOverrides()
    {
        var resources = new[]
        {
            new JsonTextResource("App", "en-US", new Dictionary<string, string>
            {
                ["Menu"] = "Menu",
                ["Only_En"] = "EnOnly",
            }),
            new JsonTextResource("App", "zh-Hans", new Dictionary<string, string>
            {
                ["Menu"] = "菜单",
            }),
        };
        var contributor = new JsonTextResourceContributor(resources, priority: 0);
        var request = new TextFillRequest("App", new LocalizationContext("zh-Hans"), ["zh-Hans", "en-US"]);
        var texts = new Dictionary<string, LocalizedText>();

        await contributor.FillAsync(request, texts);

        texts["Menu"].Value.Should().Be("菜单");
        texts["Only_En"].Value.Should().Be("EnOnly");
    }

    [Fact]
    public void GetAll_HigherPriorityCultureOverrides()
    {
        var resources = new[]
        {
            new JsonTextResource("App", "en-US", new Dictionary<string, string>
            {
                ["Menu"] = "Menu",
                ["Only_En"] = "EnOnly",
            }),
            new JsonTextResource("App", "zh-Hans", new Dictionary<string, string>
            {
                ["Menu"] = "菜单",
            }),
        };
        var snapshot = new StaticTextSnapshot(resources);

        var result = snapshot.GetAll("App", ["zh-Hans", "en-US"]);

        result["Menu"].Value.Should().Be("菜单");
        result["Only_En"].Value.Should().Be("EnOnly");
    }

    [Fact]
    public void StaticSnapshot_MergesMultipleFilesForSameCulture()
    {
        var resources = new[]
        {
            new JsonTextResource("App", "en-US", new Dictionary<string, string>
            {
                ["A"] = "1",
                ["B"] = "2",
            }),
            new JsonTextResource("App", "en-US", new Dictionary<string, string>
            {
                ["B"] = "22",
                ["C"] = "3",
            }),
        };
        var snapshot = new StaticTextSnapshot(resources);

        snapshot.Find("App", "B", ["en-US"])!.Value.Should().Be("22");
        snapshot.Find("App", "A", ["en-US"])!.Value.Should().Be("1");
        snapshot.Find("App", "C", ["en-US"])!.Value.Should().Be("3");
    }
}
