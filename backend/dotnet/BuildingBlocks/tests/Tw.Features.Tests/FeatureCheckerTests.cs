using AwesomeAssertions;
using Xunit;

namespace Tw.Features.Tests;

public sealed class FeatureCheckerTests
{
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

    private sealed class InMemoryFeatureStore(IEnumerable<FeatureValue> values) : IFeatureStore
    {
        private readonly Dictionary<FeatureCacheKey, FeatureValue> _values = values.ToDictionary(
            value => new FeatureCacheKey(value.Name, value.Scope, value.ScopeKey));

        public Task<FeatureValue?> FindAsync(
            string name,
            FeatureScope scope,
            string scopeKey,
            CancellationToken cancellationToken)
        {
            var key = new FeatureCacheKey(name, scope, scopeKey);
            return Task.FromResult(_values.GetValueOrDefault(key));
        }

        public void Replace(FeatureValue value)
        {
            _values[new FeatureCacheKey(value.Name, value.Scope, value.ScopeKey)] = value;
        }
    }

    private sealed class InMemoryFeatureCache : IFeatureCache
    {
        private readonly Dictionary<FeatureCacheKey, FeatureValue> _values = new();

        public Task<FeatureValue?> GetAsync(FeatureCacheKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult(_values.GetValueOrDefault(key));
        }

        public Task SetAsync(FeatureCacheKey key, FeatureValue value, CancellationToken cancellationToken)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(FeatureCacheKey key, CancellationToken cancellationToken)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }
    }
}
