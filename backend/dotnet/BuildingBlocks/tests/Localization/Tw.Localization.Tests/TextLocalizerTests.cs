using AwesomeAssertions;
using Tw.Localization.Json;
using Tw.Localization.Tests.Fakes;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 测试 TextLocalizer 的文本查找与回退行为
/// </summary>
public class TextLocalizerTests
{
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
        var localizer = new TextLocalizer([staticContributor, dynamicContributor], options);

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
        var localizer = new TextLocalizer([], options);

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
        var localizer = new TextLocalizer([staticContributor, dynamicContributor], options);

        var result = await localizer.GetAllAsync(
            "App",
            new LocalizationContext("zh-Hans"),
            TestContext.Current.CancellationToken);

        result.Single(t => t.Name == "Menu").Value.Should().Be("动态菜单");
        result.Single(t => t.Name == "Title").Value.Should().Be("动态标题");
        result.Should().HaveCount(2);
    }

    /// <summary>
    /// 验证调用方显式提供的已取消令牌会传递给文本贡献者
    /// </summary>
    [Fact]
    public async Task GetAsync_ForwardsExplicitCanceledTokenToContributor()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US" } };
        var contributor = new CancellationAwareTextContributor();
        var localizer = new TextLocalizer([contributor], options);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var act = async () => await localizer.GetAsync(
            "App",
            "Menu",
            new LocalizationContext("en-US"),
            cancellationTokenSource.Token);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.CancellationToken.Should().Be(cancellationTokenSource.Token);
        contributor.ObservedCancellationToken.Should().Be(cancellationTokenSource.Token);
    }

    /// <summary>
    /// 记录本地化编排器传入的令牌，并在已取消时模拟可取消的异步贡献者
    /// </summary>
    private sealed class CancellationAwareTextContributor : ITextResourceContributor
    {
        /// <summary>
        /// 最近一次调用接收的取消令牌
        /// </summary>
        public CancellationToken ObservedCancellationToken { get; private set; }

        /// <inheritdoc />
        public int Priority => 0;

        /// <inheritdoc />
        public ValueTask<LocalizedText?> GetOrNullAsync(
            Requests.TextLookupRequest request,
            CancellationToken cancellationToken = default)
        {
            ObservedCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<LocalizedText?>(null);
        }

        /// <inheritdoc />
        public ValueTask FillAsync(
            Requests.TextFillRequest request,
            IDictionary<string, LocalizedText> texts,
            CancellationToken cancellationToken = default)
        {
            ObservedCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }
}
