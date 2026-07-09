using AwesomeAssertions;
using Microsoft.Extensions.Localization;
using Tw.Localization.Json;
using Xunit;

namespace Tw.Localization.AspNetCore.Tests;

public class TwStringLocalizerTests
{
    // 用于工厂和泛型本地化器测试的私有标记类型
    private sealed class SampleResource { }

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

    // Fix 3 - 测试 1：this[name, args] 对已找到的模板正确格式化
    [Fact]
    public void FormattingIndexer_FormatFoundTemplate()
    {
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource("App", "zh-Hans", new Dictionary<string, string> { ["Greeting"] = "你好 {0}" })]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new TwStringLocalizer(snapshot, accessor, options, "App");

        var value = localizer["Greeting", "张三"];

        value.Value.Should().Be("你好 张三");
        value.ResourceNotFound.Should().BeFalse();
    }

    // Fix 3 - 测试 2：this[name, args] 对缺失键返回键名（不格式化，不抛异常）
    [Fact]
    public void FormattingIndexer_MissingKey_ReturnsKeyUnformattedWithoutThrowing()
    {
        var snapshot = new StaticTextSnapshot([]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new TwStringLocalizer(snapshot, accessor, options, "App");

        // 键名含大括号，若对键名本身调用 string.Format 会抛出 FormatException
        var act = () => localizer["Key.{missing}", "arg1"];
        act.Should().NotThrow();

        var value = localizer["Key.{missing}", "arg1"];
        value.Value.Should().Be("Key.{missing}");
        value.ResourceNotFound.Should().BeTrue();
    }

    // Fix 3 - 测试 3：GetAllStrings(true) 返回包含父级/默认文化的全集
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
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new TwStringLocalizer(snapshot, accessor, options, "App");

        var all = localizer.GetAllStrings(includeParentCultures: true).ToList();
        var keys = all.Select(s => s.Name).ToHashSet();

        // 应包含 zh-Hans 的 Hello 以及 en-US 回退链带来的 World
        keys.Should().Contain("Hello");
        keys.Should().Contain("World");
    }

    // Fix 3 - 测试 4：GetAllStrings(false) 仅返回当前文化条目
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
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new TwStringLocalizer(snapshot, accessor, options, "App");

        var all = localizer.GetAllStrings(includeParentCultures: false).ToList();
        var keys = all.Select(s => s.Name).ToHashSet();

        // 仅限 zh-Hans 条目，不应包含 en-US 独有的键
        keys.Should().Contain("Hello");
        keys.Should().NotContain("OnlyEnUs");
    }

    // Fix 3 - 测试 5：accessor.Current 为 null 时回退到 options.DefaultCulture
    [Fact]
    public void Indexer_AccessorCurrentNull_FallsBackToDefaultCulture()
    {
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource("App", "en-US", new Dictionary<string, string> { ["Title"] = "Title" })]);
        // accessor.Current 故意保持 null
        var accessor = new CurrentLocalizationContextAccessor { Current = null };
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new TwStringLocalizer(snapshot, accessor, options, "App");

        var value = localizer["Title"];

        value.Value.Should().Be("Title");
        value.ResourceNotFound.Should().BeFalse();
    }

    // Fix 3 - 测试 6：TwStringLocalizerFactory.Create(Type) 绑定到类型简单名
    [Fact]
    public void Factory_CreateByType_BindsToSimpleName()
    {
        var resourceName = typeof(SampleResource).Name; // "SampleResource"
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource(resourceName, "zh-Hans", new Dictionary<string, string> { ["Key"] = "值" })]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var factory = new TwStringLocalizerFactory(snapshot, accessor, options);

        var localizer = factory.Create(typeof(SampleResource));
        var value = localizer["Key"];

        value.Value.Should().Be("值");
        value.ResourceNotFound.Should().BeFalse();
    }

    // Fix 3 - 测试 7：TwStringLocalizerFactory.Create(string, string) 绑定到 baseName，忽略 location
    [Fact]
    public void Factory_CreateByBaseName_IgnoresLocation()
    {
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource("App", "zh-Hans", new Dictionary<string, string> { ["Footer"] = "页脚" })]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var factory = new TwStringLocalizerFactory(snapshot, accessor, options);

        var localizer = factory.Create("App", "ignored-location");
        var value = localizer["Footer"];

        value.Value.Should().Be("页脚");
        value.ResourceNotFound.Should().BeFalse();
    }

    // Fix 3 - 测试 8：TwStringLocalizer<TResource> 泛型类委托给工厂创建的 localizer
    [Fact]
    public void GenericLocalizer_DelegatesToFactory()
    {
        var resourceName = typeof(SampleResource).Name; // "SampleResource"
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource(resourceName, "zh-Hans", new Dictionary<string, string> { ["Name"] = "名称" })]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var factory = new TwStringLocalizerFactory(snapshot, accessor, options);

        IStringLocalizer<SampleResource> localizer = new TwStringLocalizer<SampleResource>(factory);
        var value = localizer["Name"];

        value.Value.Should().Be("名称");
        value.ResourceNotFound.Should().BeFalse();
    }
}
