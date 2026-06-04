using FluentAssertions;
using Tw.Context;
using Tw.Localization.Json;
using Tw.Localization.Tests.Fakes;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 测试 TextLocalizer 的文本查找与回退行为
/// </summary>
public class TextLocalizerTests
{
    private static NullCancellationTokenProvider CreateTokenProvider() => new(new AsyncLocalCancellationTokenScopeProvider());

    [Fact]
    public async Task GetAsync_PrefersDynamicTenantText()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var staticContributor = new JsonTextResourceContributor(
            [new JsonTextResource("App", "zh-Hans", new Dictionary<string, string> { ["Menu"] = "静态菜单" })],
            priority: 0);
        var store = new InMemoryDynamicTextStore();
        store.Add(new LocalizedText("App", "Menu", "租户菜单", "zh-Hans", false, LocalizedTextSource.Dynamic), tenantId: "t1");
        var dynamicContributor = new DynamicTextContributor(store, priority: 100);
        var localizer = new TextLocalizer([staticContributor, dynamicContributor], options, CreateTokenProvider());

        var text = await localizer.GetAsync("App", "Menu", new LocalizationContext("zh-Hans") { TenantId = "t1" });

        text.Value.Should().Be("租户菜单");
    }

    [Fact]
    public async Task GetAsync_ReturnsNotFoundText_WhenMissing()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US" } };
        var localizer = new TextLocalizer([], options, CreateTokenProvider());

        var text = await localizer.GetAsync("App", "Missing", new LocalizationContext("en-US"));

        text.ResourceNotFound.Should().BeTrue();
        text.Value.Should().Be("Missing");
    }
}
