using AwesomeAssertions;
using Tw.Localization.Json;
using Tw.Localization.Requests;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 覆盖JSONText资源Contributor的核心行为和边界条件
/// </summary>
public class JsonTextResourceContributorTests
{
    /// <summary>
    /// 验证读取Or空值异步返回Current文化文本
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证StaticSnapshot返回回退文化文本
    /// </summary>
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

    /// <summary>
    /// 验证Fill异步HigherPriority文化Overrides
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证读取AllHigherPriority文化Overrides
    /// </summary>
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

    /// <summary>
    /// 验证StaticSnapshotMergesMultipleFiles针对Same文化
    /// </summary>
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
