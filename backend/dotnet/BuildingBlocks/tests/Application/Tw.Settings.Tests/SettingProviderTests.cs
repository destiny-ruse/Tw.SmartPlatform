using AwesomeAssertions;
using Xunit;

namespace Tw.Settings.Tests;

public sealed class SettingProviderTests
{
    [Fact]
    public async Task GetAsync_UsesUserTenantServiceThenDefaultFallback()
    {
        var store = new InMemorySettingStore([
            new SettingValue("orders.page-size", SettingScope.Service, "order-service", "20", 1),
            new SettingValue("orders.page-size", SettingScope.Tenant, "tenant-a", "50", 2),
            new SettingValue("orders.page-size", SettingScope.User, "user-a", "100", 3)
        ]);
        var provider = new SettingProvider(store, new InMemorySettingCache(), [
            new SettingDefinition("orders.page-size", "10")
        ]);

        var value = await provider.GetAsync(
            "orders.page-size",
            "tenant-a",
            "order-service",
            "user-a",
            TestContext.Current.CancellationToken);

        value.Should().Be("100");
    }

    [Fact]
    public async Task RefreshAsync_RemovesMatchingCachedValue()
    {
        var cache = new InMemorySettingCache();
        var store = new InMemorySettingStore([
            new SettingValue("orders.page-size", SettingScope.User, "user-a", "100", 1)
        ]);
        var provider = new SettingProvider(store, cache);

        await provider.GetAsync(
            "orders.page-size",
            "tenant-a",
            "order-service",
            "user-a",
            TestContext.Current.CancellationToken);
        store.Replace(new SettingValue("orders.page-size", SettingScope.User, "user-a", "200", 2));

        var cached = await provider.GetAsync(
            "orders.page-size",
            "tenant-a",
            "order-service",
            "user-a",
            TestContext.Current.CancellationToken);
        cached.Should().Be("100");

        await provider.RefreshAsync(
            new SettingRefreshRequest("orders.page-size", SettingScope.User, "user-a"),
            TestContext.Current.CancellationToken);

        var refreshed = await provider.GetAsync(
            "orders.page-size",
            "tenant-a",
            "order-service",
            "user-a",
            TestContext.Current.CancellationToken);

        refreshed.Should().Be("200");
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenValueAndDefinitionMissing()
    {
        var provider = new SettingProvider(new InMemorySettingStore([]), new InMemorySettingCache());

        var value = await provider.GetAsync(
            "orders.page-size",
            "tenant-a",
            "order-service",
            userId: null,
            TestContext.Current.CancellationToken);

        value.Should().BeNull();
    }

    private sealed class InMemorySettingStore(IEnumerable<SettingValue> values) : ISettingStore
    {
        private readonly Dictionary<SettingCacheKey, SettingValue> _values = values.ToDictionary(
            value => new SettingCacheKey(value.Name, value.Scope, value.ScopeKey));

        public Task<SettingValue?> FindAsync(
            string name,
            SettingScope scope,
            string scopeKey,
            CancellationToken cancellationToken)
        {
            var key = new SettingCacheKey(name, scope, scopeKey);
            return Task.FromResult(_values.GetValueOrDefault(key));
        }

        public void Replace(SettingValue value)
        {
            _values[new SettingCacheKey(value.Name, value.Scope, value.ScopeKey)] = value;
        }
    }

    private sealed class InMemorySettingCache : ISettingCache
    {
        private readonly Dictionary<SettingCacheKey, SettingValue> _values = new();

        public Task<SettingValue?> GetAsync(SettingCacheKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult(_values.GetValueOrDefault(key));
        }

        public Task SetAsync(SettingCacheKey key, SettingValue value, CancellationToken cancellationToken)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(SettingCacheKey key, CancellationToken cancellationToken)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }
    }
}
