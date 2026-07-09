using AwesomeAssertions;
using Xunit;

namespace Tw.Features.Tests;

/// <summary>验证 FeatureCheckerTests 相关行为</summary>
public sealed class FeatureCheckerTests
{
    /// <summary>验证 CheckAsync_UsesTenantOverrideBeforeServiceDefault 场景</summary>
    /// <returns>CheckAsync_UsesTenantOverrideBeforeServiceDefault 的执行结果</returns>
    [Fact]
    public async Task CheckAsync_UsesTenantOverrideBeforeServiceDefault()
    {
        var store = new InMemoryFeatureStore([
            new FeatureValue("billing.approval", FeatureScope.Service, "billing-service", true, 1),
            new FeatureValue("billing.approval", FeatureScope.Tenant, "tenant-a", false, 2)
        ]);
        var checker = new FeatureChecker(store, new InMemoryFeatureCache());

        var result = await checker.CheckAsync(
            "billing.approval",
            "tenant-a",
            "billing-service",
            TestContext.Current.CancellationToken);

        result.Enabled.Should().BeFalse();
        result.Code.Should().Be("FEATURE:000001");
    }

    /// <summary>验证 RefreshAsync_RemovesMatchingCachedValue 场景</summary>
    /// <returns>RefreshAsync_RemovesMatchingCachedValue 的执行结果</returns>
    [Fact]
    public async Task RefreshAsync_RemovesMatchingCachedValue()
    {
        var cache = new InMemoryFeatureCache();
        var store = new InMemoryFeatureStore([
            new FeatureValue("billing.approval", FeatureScope.Tenant, "tenant-a", true, 1)
        ]);
        var checker = new FeatureChecker(store, cache);

        await checker.CheckAsync(
            "billing.approval",
            "tenant-a",
            "billing-service",
            TestContext.Current.CancellationToken);
        store.Replace(new FeatureValue("billing.approval", FeatureScope.Tenant, "tenant-a", false, 2));

        var cached = await checker.CheckAsync(
            "billing.approval",
            "tenant-a",
            "billing-service",
            TestContext.Current.CancellationToken);
        cached.Enabled.Should().BeTrue();

        await checker.RefreshAsync(
            new FeatureRefreshRequest("billing.approval", FeatureScope.Tenant, "tenant-a"),
            TestContext.Current.CancellationToken);

        var refreshed = await checker.CheckAsync(
            "billing.approval",
            "tenant-a",
            "billing-service",
            TestContext.Current.CancellationToken);

        refreshed.Enabled.Should().BeFalse();
    }

    /// <summary>验证 InMemoryFeatureStore 相关行为</summary>
    private sealed class InMemoryFeatureStore(IEnumerable<FeatureValue> values) : IFeatureStore
    {
        /// <summary>表示 _values 字段</summary>
        private readonly Dictionary<FeatureCacheKey, FeatureValue> _values = values.ToDictionary(
            value => new FeatureCacheKey(value.Name, value.Scope, value.ScopeKey));

        /// <summary>验证 FindAsync 场景</summary>
        /// <param name="name">name 参数</param>
        /// <param name="scope">scope 参数</param>
        /// <param name="scopeKey">scopeKey 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>FindAsync 的执行结果</returns>
        public Task<FeatureValue?> FindAsync(
            string name,
            FeatureScope scope,
            string scopeKey,
            CancellationToken cancellationToken)
        {
            var key = new FeatureCacheKey(name, scope, scopeKey);
            return Task.FromResult(_values.GetValueOrDefault(key));
        }

        /// <summary>验证 Replace 场景</summary>
        /// <param name="value">value 参数</param>
        public void Replace(FeatureValue value)
        {
            _values[new FeatureCacheKey(value.Name, value.Scope, value.ScopeKey)] = value;
        }
    }

    /// <summary>验证 InMemoryFeatureCache 相关行为</summary>
    private sealed class InMemoryFeatureCache : IFeatureCache
    {
        /// <summary>表示 _values 字段</summary>
        private readonly Dictionary<FeatureCacheKey, FeatureValue> _values = new();

        /// <summary>验证 GetAsync 场景</summary>
        /// <param name="key">key 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>GetAsync 的执行结果</returns>
        public Task<FeatureValue?> GetAsync(FeatureCacheKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult(_values.GetValueOrDefault(key));
        }

        /// <summary>验证 SetAsync 场景</summary>
        /// <param name="key">key 参数</param>
        /// <param name="value">value 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>SetAsync 的执行结果</returns>
        public Task SetAsync(FeatureCacheKey key, FeatureValue value, CancellationToken cancellationToken)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        /// <summary>验证 RemoveAsync 场景</summary>
        /// <param name="key">key 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>RemoveAsync 的执行结果</returns>
        public Task RemoveAsync(FeatureCacheKey key, CancellationToken cancellationToken)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }
    }
}
