using AwesomeAssertions;
using Tw.Localization.Requests;
using Tw.Localization.Tests.Fakes;
using Tw.Threading;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 测试 EntityTranslationService 的批量翻译查找与文化/租户回退行为
/// </summary>
public class EntityTranslationServiceTests
{
    /// <summary>
    /// 创建令牌提供器测试对象
    /// </summary>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static NullCancellationTokenProvider CreateTokenProvider() => new(new AsyncLocalCancellationTokenScopeProvider());

    /// <summary>
    /// 验证读取Fields异步UsesBatch存储和回退
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task GetFieldsAsync_UsesBatchStoreAndFallback()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh", "zh-Hans" } };
        var store = new InMemoryEntityTranslationStore();
        store.Add(new EntityTranslation("Product", "42", "Name", "zh", "父级名称", "t1"));
        var service = new EntityTranslationService(store, options, CreateTokenProvider());
        var query = new EntityTranslationBatchQuery(
            [new EntityTranslationKey("Product", "42", "Name")],
            new LocalizationContext("zh-Hans") { TenantId = "t1" });

        var result = await service.GetFieldsAsync(query, TestContext.Current.CancellationToken);

        result[new EntityTranslationKey("Product", "42", "Name")].Value.Should().Be("父级名称");
        store.GetListCallCount.Should().Be(1);
    }

    /// <summary>
    /// 验证读取Field异步返回空值当缺少
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task GetFieldAsync_ReturnsNull_WhenMissing()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US" } };
        var store = new InMemoryEntityTranslationStore();
        var service = new EntityTranslationService(store, options, CreateTokenProvider());

        var result = await service.GetFieldAsync(
            new EntityTranslationLookup(
                new EntityTranslationKey("Product", "42", "Name"),
                new LocalizationContext("en-US")),
            TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    /// <summary>
    /// 验证读取Fields异步PrefersCurrent租户OverGlobal
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task GetFieldsAsync_PrefersCurrentTenantOverGlobal()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var store = new InMemoryEntityTranslationStore();
        store.Add(new EntityTranslation("Product", "42", "Name", "zh-Hans", "全局名称", null));
        store.Add(new EntityTranslation("Product", "42", "Name", "zh-Hans", "租户名称", "t1"));
        var service = new EntityTranslationService(store, options, CreateTokenProvider());
        var key = new EntityTranslationKey("Product", "42", "Name");
        var query = new EntityTranslationBatchQuery(
            [key],
            new LocalizationContext("zh-Hans") { TenantId = "t1" });

        var result = await service.GetFieldsAsync(query, TestContext.Current.CancellationToken);

        result[key].Value.Should().Be("租户名称");
    }

    /// <summary>
    /// 验证读取Fields异步Falls回到Global当租户缺少
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task GetFieldsAsync_FallsBackToGlobal_WhenTenantMissing()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var store = new InMemoryEntityTranslationStore();
        store.Add(new EntityTranslation("Product", "42", "Name", "zh-Hans", "全局名称", null));
        var service = new EntityTranslationService(store, options, CreateTokenProvider());
        var key = new EntityTranslationKey("Product", "42", "Name");
        var query = new EntityTranslationBatchQuery(
            [key],
            new LocalizationContext("zh-Hans") { TenantId = "t1" });

        var result = await service.GetFieldsAsync(query, TestContext.Current.CancellationToken);

        result[key].Value.Should().Be("全局名称");
    }
}
