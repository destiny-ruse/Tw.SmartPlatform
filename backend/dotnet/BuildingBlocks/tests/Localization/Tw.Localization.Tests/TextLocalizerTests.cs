using AwesomeAssertions;
using Tw.Localization.Json;
using Tw.Localization.Tests.Fakes;
using Tw.Threading;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 测试 TextLocalizer 的文本查找与回退行为
/// </summary>
public class TextLocalizerTests
{
    /// <summary>
    /// 创建令牌提供器测试对象
    /// </summary>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static NullCancellationTokenProvider CreateTokenProvider() => new(new AsyncLocalCancellationTokenScopeProvider());

    /// <summary>
    /// 验证读取异步Prefers动态租户文本
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

        var text = await localizer.GetAsync(
            "App",
            "Menu",
            new LocalizationContext("zh-Hans") { TenantId = "t1" },
            TestContext.Current.CancellationToken);

        text.Value.Should().Be("租户菜单");
    }

    /// <summary>
    /// 验证读取异步返回不Found文本当缺少
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task GetAsync_ReturnsNotFoundText_WhenMissing()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US" } };
        var localizer = new TextLocalizer([], options, CreateTokenProvider());

        var text = await localizer.GetAsync(
            "App",
            "Missing",
            new LocalizationContext("en-US"),
            TestContext.Current.CancellationToken);

        text.ResourceNotFound.Should().BeTrue();
        text.Value.Should().Be("Missing");
    }

    /// <summary>
    /// 验证读取All异步HigherPriorityContributorOverrides
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task GetAllAsync_HigherPriorityContributorOverrides()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var staticContributor = new JsonTextResourceContributor(
            [new JsonTextResource("App", "zh-Hans", new Dictionary<string, string> { ["Menu"] = "静态菜单" })],
            priority: 0);
        var store = new InMemoryDynamicTextStore();
        store.Add(new LocalizedText("App", "Menu", "动态菜单", "zh-Hans", false, LocalizedTextSource.Dynamic));
        store.Add(new LocalizedText("App", "Title", "动态标题", "zh-Hans", false, LocalizedTextSource.Dynamic));
        var dynamicContributor = new DynamicTextContributor(store, priority: 100);
        var localizer = new TextLocalizer([staticContributor, dynamicContributor], options, CreateTokenProvider());

        var result = await localizer.GetAllAsync(
            "App",
            new LocalizationContext("zh-Hans"),
            TestContext.Current.CancellationToken);

        result.Single(t => t.Name == "Menu").Value.Should().Be("动态菜单");
        result.Single(t => t.Name == "Title").Value.Should().Be("动态标题");
        result.Should().HaveCount(2);
    }
}
