using AwesomeAssertions;
using Xunit;

namespace Tw.Features.Tests;

/// <summary>
/// 覆盖功能Checker的核心行为和边界条件
/// </summary>
public sealed class FeatureCheckerTests
{
    /// <summary>
    /// 验证Check异步Uses租户Override前置处理服务默认
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证Refresh异步RemovesMatchingCached值
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 覆盖InMemory功能存储的核心行为和边界条件
    /// </summary>
    private sealed class InMemoryFeatureStore(IEnumerable<FeatureValue> values) : IFeatureStore
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的values
        /// </summary>
        private readonly Dictionary<FeatureCacheKey, FeatureValue> _values = values.ToDictionary(
            value => new FeatureCacheKey(value.Name, value.Scope, value.ScopeKey));

        /// <summary>
        /// 说明查找Async在当前类型中的职责
        /// </summary>
        /// <param name="name">待匹配成员或资源的名称</param>
        /// <param name="scope">功能值生效的作用域</param>
        /// <param name="scopeKey">作用域内定位主体或租户的键</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的功能值</returns>
        public Task<FeatureValue?> FindAsync(
            string name,
            FeatureScope scope,
            string scopeKey,
            CancellationToken cancellationToken)
        {
            var key = new FeatureCacheKey(name, scope, scopeKey);
            return Task.FromResult(_values.GetValueOrDefault(key));
        }

        /// <summary>
        /// 替换测试替身中保存的条目集合
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        public void Replace(FeatureValue value)
        {
            _values[new FeatureCacheKey(value.Name, value.Scope, value.ScopeKey)] = value;
        }
    }

    /// <summary>
    /// 覆盖InMemory功能缓存的核心行为和边界条件
    /// </summary>
    private sealed class InMemoryFeatureCache : IFeatureCache
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的values
        /// </summary>
        private readonly Dictionary<FeatureCacheKey, FeatureValue> _values = new();

        /// <summary>
        /// 从测试替身中读取指定条目
        /// </summary>
        /// <param name="key">用于定位目标数据或缓存项的键</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的功能值</returns>
        public Task<FeatureValue?> GetAsync(FeatureCacheKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult(_values.GetValueOrDefault(key));
        }

        /// <summary>
        /// 将指定条目写入测试替身
        /// </summary>
        /// <param name="key">用于定位目标数据或缓存项的键</param>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task SetAsync(FeatureCacheKey key, FeatureValue value, CancellationToken cancellationToken)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 从测试替身中移除指定条目
        /// </summary>
        /// <param name="key">用于定位目标数据或缓存项的键</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task RemoveAsync(FeatureCacheKey key, CancellationToken cancellationToken)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }
    }
}
