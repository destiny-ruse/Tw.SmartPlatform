using AwesomeAssertions;
using Tw.Threading;
using Tw.Localization.Requests;
using Tw.Localization.Tests.Fakes;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 测试 EntityTranslationService 的批量翻译查找与文化/租户回退行为
/// </summary>
public class EntityTranslationServiceTests
{
    /// <summary>验证 CreateTokenProvider 场景</summary>
    /// <returns>CreateTokenProvider 的执行结果</returns>
    private static NullCancellationTokenProvider CreateTokenProvider() => new(new AsyncLocalCancellationTokenScopeProvider());

    /// <summary>验证 GetFieldsAsync_UsesBatchStoreAndFallback 场景</summary>
    /// <returns>GetFieldsAsync_UsesBatchStoreAndFallback 的执行结果</returns>
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

        var result = await service.GetFieldsAsync(query);

        result[new EntityTranslationKey("Product", "42", "Name")].Value.Should().Be("父级名称");
        store.GetListCallCount.Should().Be(1);
    }

    /// <summary>验证 GetFieldAsync_ReturnsNull_WhenMissing 场景</summary>
    /// <returns>GetFieldAsync_ReturnsNull_WhenMissing 的执行结果</returns>
    [Fact]
    public async Task GetFieldAsync_ReturnsNull_WhenMissing()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US" } };
        var store = new InMemoryEntityTranslationStore();
        var service = new EntityTranslationService(store, options, CreateTokenProvider());

        var result = await service.GetFieldAsync(
            new EntityTranslationLookup(
                new EntityTranslationKey("Product", "42", "Name"),
                new LocalizationContext("en-US")));

        result.Should().BeNull();
    }

    /// <summary>验证 GetFieldsAsync_PrefersCurrentTenantOverGlobal 场景</summary>
    /// <returns>GetFieldsAsync_PrefersCurrentTenantOverGlobal 的执行结果</returns>
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

        var result = await service.GetFieldsAsync(query);

        result[key].Value.Should().Be("租户名称");
    }

    /// <summary>验证 GetFieldsAsync_FallsBackToGlobal_WhenTenantMissing 场景</summary>
    /// <returns>GetFieldsAsync_FallsBackToGlobal_WhenTenantMissing 的执行结果</returns>
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

        var result = await service.GetFieldsAsync(query);

        result[key].Value.Should().Be("全局名称");
    }
}
