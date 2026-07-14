using AwesomeAssertions;
using Microsoft.Extensions.Localization;
using Tw.Localization;
using Tw.Localization.Json;
using CoreLocalizationOptions = Tw.Localization.LocalizationOptions;
using Xunit;

namespace Tw.AspNetCore.Localization.Tests;

/// <summary>
/// 覆盖静态快照字符串本地化器的核心行为和边界条件
/// </summary>
public sealed class StaticSnapshotStringLocalizerTests
{
    /// <summary>
    /// 标识工厂与泛型本地化器测试使用的资源范围
    /// </summary>
    private sealed class SampleResource
    {
    }

    /// <summary>
    /// 索引器返回静态快照中命中的文本
    /// </summary>
    [Fact]
    public void Indexer_ReturnsStaticSnapshotText()
    {
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource("App", "zh-Hans", new Dictionary<string, string> { ["Menu"] = "菜单" })]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new CoreLocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new StaticSnapshotStringLocalizer(snapshot, accessor, options, "App");

        var value = localizer["Menu"];

        value.Value.Should().Be("菜单");
        value.ResourceNotFound.Should().BeFalse();
    }

    /// <summary>
    /// 索引器在文本缺失时返回原始键
    /// </summary>
    [Fact]
    public void Indexer_ReturnsKeyForMissingText()
    {
        var snapshot = new StaticTextSnapshot([]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new CoreLocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new StaticSnapshotStringLocalizer(snapshot, accessor, options, "App");

        var value = localizer["Missing"];

        value.Value.Should().Be("Missing");
        value.ResourceNotFound.Should().BeTrue();
    }

    /// <summary>
    /// 格式化索引器使用调用参数格式化命中的模板
    /// </summary>
    [Fact]
    public void FormattingIndexer_FormatFoundTemplate()
    {
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource("App", "zh-Hans", new Dictionary<string, string> { ["Greeting"] = "你好 {0}" })]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new CoreLocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new StaticSnapshotStringLocalizer(snapshot, accessor, options, "App");

        var value = localizer["Greeting", "张三"];

        value.Value.Should().Be("你好 张三");
        value.ResourceNotFound.Should().BeFalse();
    }

    /// <summary>
    /// 格式化索引器在文本缺失时返回未格式化的原始键
    /// </summary>
    [Fact]
    public void FormattingIndexer_MissingKey_ReturnsKeyUnformattedWithoutThrowing()
    {
        var snapshot = new StaticTextSnapshot([]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new CoreLocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new StaticSnapshotStringLocalizer(snapshot, accessor, options, "App");

        // 键名含大括号，若对键名本身调用 string.Format 会抛出 FormatException
        var act = () => localizer["Key.{missing}", "arg1"];
        act.Should().NotThrow();

        var value = localizer["Key.{missing}", "arg1"];
        value.Value.Should().Be("Key.{missing}");
        value.ResourceNotFound.Should().BeTrue();
    }

    /// <summary>
    /// 包含父级文化时返回当前文化与默认文化的合并结果
    /// </summary>
    [Fact]
    public void GetAllStrings_IncludeParentCultures_ReturnsMergedSet()
    {
        var snapshot = new StaticTextSnapshot(
        [
            new JsonTextResource("App", "en-US", new Dictionary<string, string>
            {
                ["Hello"] = "Hello",
                ["World"] = "World"
            }),
            new JsonTextResource("App", "zh-Hans", new Dictionary<string, string>
            {
                ["Hello"] = "你好"
            })
        ]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new CoreLocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new StaticSnapshotStringLocalizer(snapshot, accessor, options, "App");

        var all = localizer.GetAllStrings(includeParentCultures: true).ToList();
        var keys = all.Select(s => s.Name).ToHashSet();

        // 应包含 zh-Hans 的 Hello 以及 en-US 回退链带来的 World
        keys.Should().Contain("Hello");
        keys.Should().Contain("World");
    }

    /// <summary>
    /// 不包含父级文化时只返回当前文化的条目
    /// </summary>
    [Fact]
    public void GetAllStrings_ExcludeParentCultures_RestrictsToCurrentCulture()
    {
        var snapshot = new StaticTextSnapshot(
        [
            new JsonTextResource("App", "en-US", new Dictionary<string, string>
            {
                ["Hello"] = "Hello",
                ["OnlyEnUs"] = "only in en-US"
            }),
            new JsonTextResource("App", "zh-Hans", new Dictionary<string, string>
            {
                ["Hello"] = "你好"
            })
        ]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new CoreLocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new StaticSnapshotStringLocalizer(snapshot, accessor, options, "App");

        var all = localizer.GetAllStrings(includeParentCultures: false).ToList();
        var keys = all.Select(s => s.Name).ToHashSet();

        // 仅限 zh-Hans 条目，不应包含 en-US 独有的键
        keys.Should().Contain("Hello");
        keys.Should().NotContain("OnlyEnUs");
    }

    /// <summary>
    /// 请求上下文缺失时索引器回退到默认文化
    /// </summary>
    [Fact]
    public void Indexer_AccessorCurrentNull_FallsBackToDefaultCulture()
    {
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource("App", "en-US", new Dictionary<string, string> { ["Title"] = "Title" })]);
        // accessor.Current 故意保持 null
        var accessor = new CurrentLocalizationContextAccessor { Current = null };
        var options = new CoreLocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new StaticSnapshotStringLocalizer(snapshot, accessor, options, "App");

        var value = localizer["Title"];

        value.Value.Should().Be("Title");
        value.ResourceNotFound.Should().BeFalse();
    }

    /// <summary>
    /// 工厂使用资源类型的简单名称定位静态快照资源
    /// </summary>
    [Fact]
    public void Factory_CreateByType_BindsToSimpleName()
    {
        var resourceName = typeof(SampleResource).Name;
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource(resourceName, "zh-Hans", new Dictionary<string, string> { ["Key"] = "值" })]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new CoreLocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var factory = new StaticSnapshotStringLocalizerFactory(snapshot, accessor, options);

        var localizer = factory.Create(typeof(SampleResource));
        var value = localizer["Key"];

        value.Value.Should().Be("值");
        value.ResourceNotFound.Should().BeFalse();
    }

    /// <summary>
    /// 工厂使用资源基础名称定位资源并忽略程序集位置
    /// </summary>
    [Fact]
    public void Factory_CreateByBaseName_IgnoresLocation()
    {
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource("App", "zh-Hans", new Dictionary<string, string> { ["Footer"] = "页脚" })]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new CoreLocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var factory = new StaticSnapshotStringLocalizerFactory(snapshot, accessor, options);

        var localizer = factory.Create("App", "ignored-location");
        var value = localizer["Footer"];

        value.Value.Should().Be("页脚");
        value.ResourceNotFound.Should().BeFalse();
    }

    /// <summary>
    /// 泛型本地化器委托给工厂创建的资源专用本地化器
    /// </summary>
    [Fact]
    public void GenericLocalizer_DelegatesToFactory()
    {
        var resourceName = typeof(SampleResource).Name;
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource(resourceName, "zh-Hans", new Dictionary<string, string> { ["Name"] = "名称" })]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new CoreLocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var factory = new StaticSnapshotStringLocalizerFactory(snapshot, accessor, options);

        IStringLocalizer<SampleResource> localizer = new StaticSnapshotStringLocalizer<SampleResource>(factory);
        var value = localizer["Name"];

        value.Value.Should().Be("名称");
        value.ResourceNotFound.Should().BeFalse();
    }
}
