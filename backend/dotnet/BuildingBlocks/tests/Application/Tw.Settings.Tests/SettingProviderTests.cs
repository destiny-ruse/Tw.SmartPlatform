using AwesomeAssertions;
using Xunit;

namespace Tw.Settings.Tests;

/// <summary>
/// 覆盖设置提供器的核心行为和边界条件
/// </summary>
public sealed class SettingProviderTests
{
    /// <summary>
    /// 验证读取异步Uses用户租户服务Then默认回退
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证Refresh异步RemovesMatchingCached值
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证读取异步返回空值当值和定义缺少
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 覆盖InMemory设置存储的核心行为和边界条件
    /// </summary>
    private sealed class InMemorySettingStore(IEnumerable<SettingValue> values) : ISettingStore
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的values
        /// </summary>
        private readonly Dictionary<SettingCacheKey, SettingValue> _values = values.ToDictionary(
            value => new SettingCacheKey(value.Name, value.Scope, value.ScopeKey));

        /// <summary>
        /// 说明查找Async在当前类型中的职责
        /// </summary>
        /// <param name="name">待匹配成员或资源的名称</param>
        /// <param name="scope">功能值生效的作用域</param>
        /// <param name="scopeKey">作用域内定位主体或租户的键</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的设置值</returns>
        public Task<SettingValue?> FindAsync(
            string name,
            SettingScope scope,
            string scopeKey,
            CancellationToken cancellationToken)
        {
            var key = new SettingCacheKey(name, scope, scopeKey);
            return Task.FromResult(_values.GetValueOrDefault(key));
        }

        /// <summary>
        /// 替换测试替身中保存的条目集合
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        public void Replace(SettingValue value)
        {
            _values[new SettingCacheKey(value.Name, value.Scope, value.ScopeKey)] = value;
        }
    }

    /// <summary>
    /// 覆盖InMemory设置缓存的核心行为和边界条件
    /// </summary>
    private sealed class InMemorySettingCache : ISettingCache
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的values
        /// </summary>
        private readonly Dictionary<SettingCacheKey, SettingValue> _values = new();

        /// <summary>
        /// 从测试替身中读取指定条目
        /// </summary>
        /// <param name="key">用于定位目标数据或缓存项的键</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的设置值</returns>
        public Task<SettingValue?> GetAsync(SettingCacheKey key, CancellationToken cancellationToken)
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
        public Task SetAsync(SettingCacheKey key, SettingValue value, CancellationToken cancellationToken)
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
        public Task RemoveAsync(SettingCacheKey key, CancellationToken cancellationToken)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }
    }
}
