using AwesomeAssertions;
using Xunit;

namespace Tw.Settings.Tests;

/// <summary>验证 SettingProviderTests 相关行为</summary>
public sealed class SettingProviderTests
{
    /// <summary>验证 GetAsync_UsesUserTenantServiceThenDefaultFallback 场景</summary>
    /// <returns>GetAsync_UsesUserTenantServiceThenDefaultFallback 的执行结果</returns>
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

    /// <summary>验证 RefreshAsync_RemovesMatchingCachedValue 场景</summary>
    /// <returns>RefreshAsync_RemovesMatchingCachedValue 的执行结果</returns>
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

    /// <summary>验证 GetAsync_ReturnsNull_WhenValueAndDefinitionMissing 场景</summary>
    /// <returns>GetAsync_ReturnsNull_WhenValueAndDefinitionMissing 的执行结果</returns>
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

    /// <summary>验证 InMemorySettingStore 相关行为</summary>
    private sealed class InMemorySettingStore(IEnumerable<SettingValue> values) : ISettingStore
    {
        /// <summary>表示 _values 字段</summary>
        private readonly Dictionary<SettingCacheKey, SettingValue> _values = values.ToDictionary(
            value => new SettingCacheKey(value.Name, value.Scope, value.ScopeKey));

        /// <summary>验证 FindAsync 场景</summary>
        /// <param name="name">name 参数</param>
        /// <param name="scope">scope 参数</param>
        /// <param name="scopeKey">scopeKey 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>FindAsync 的执行结果</returns>
        public Task<SettingValue?> FindAsync(
            string name,
            SettingScope scope,
            string scopeKey,
            CancellationToken cancellationToken)
        {
            var key = new SettingCacheKey(name, scope, scopeKey);
            return Task.FromResult(_values.GetValueOrDefault(key));
        }

        /// <summary>验证 Replace 场景</summary>
        /// <param name="value">value 参数</param>
        public void Replace(SettingValue value)
        {
            _values[new SettingCacheKey(value.Name, value.Scope, value.ScopeKey)] = value;
        }
    }

    /// <summary>验证 InMemorySettingCache 相关行为</summary>
    private sealed class InMemorySettingCache : ISettingCache
    {
        /// <summary>表示 _values 字段</summary>
        private readonly Dictionary<SettingCacheKey, SettingValue> _values = new();

        /// <summary>验证 GetAsync 场景</summary>
        /// <param name="key">key 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>GetAsync 的执行结果</returns>
        public Task<SettingValue?> GetAsync(SettingCacheKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult(_values.GetValueOrDefault(key));
        }

        /// <summary>验证 SetAsync 场景</summary>
        /// <param name="key">key 参数</param>
        /// <param name="value">value 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>SetAsync 的执行结果</returns>
        public Task SetAsync(SettingCacheKey key, SettingValue value, CancellationToken cancellationToken)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        /// <summary>验证 RemoveAsync 场景</summary>
        /// <param name="key">key 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>RemoveAsync 的执行结果</returns>
        public Task RemoveAsync(SettingCacheKey key, CancellationToken cancellationToken)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }
    }
}
