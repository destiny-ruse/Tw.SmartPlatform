using FluentAssertions;
using Tw.Context;
using Tw.Localization.Requests;
using Tw.Localization.Tests.Fakes;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 测试 EntityTranslationService 的批量翻译查找与文化/租户回退行为
/// </summary>
public class EntityTranslationServiceTests
{
    private static NullCancellationTokenProvider CreateTokenProvider() => new(new AsyncLocalCancellationTokenScopeProvider());

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
}
