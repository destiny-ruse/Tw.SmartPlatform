using AwesomeAssertions;
using Tw.Authorization;
using Xunit;

namespace Tw.Authorization.Tests;

/// <summary>
/// 验证默认权限检查器的缓存、存储与失败传播契约
/// </summary>
public sealed class PermissionCheckerTests
{
    /// <summary>
    /// 共享调用轨迹中的缓存读取标记
    /// </summary>
    private const string CacheGetCall = "Cache.Get";

    /// <summary>
    /// 共享调用轨迹中的授权存储读取标记
    /// </summary>
    private const string StoreHasGrantCall = "Store.HasGrant";

    /// <summary>
    /// 共享调用轨迹中的缓存写入标记
    /// </summary>
    private const string CacheSetCall = "Cache.Set";

    /// <summary>
    /// 缓存允许时直接返回成功且不读取授权存储
    /// </summary>
    /// <returns>权限检查完成后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_ReturnsSuccessWithoutReadingStore_WhenCacheAllows()
    {
        var context = CreateContext();
        var cacheKey = CreateCacheKey(context);
        var cancellationToken = TestContext.Current.CancellationToken;
        var callTrace = new List<string>();
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(false), callTrace);
        var grantCache = new RecordingPermissionGrantCache(
            callTrace,
            [new KeyValuePair<PermissionGrantCacheKey, bool>(cacheKey, true)]);
        var checker = new PermissionChecker(grantStore, grantCache);

        var result = await checker.CheckAsync(context, cancellationToken);

        result.Should().Be(AuthorizationResult.Success());
        grantStore.CallCount.Should().Be(0);
        grantCache.GetCallCount.Should().Be(1);
        grantCache.SetCallCount.Should().Be(0);
        grantCache.ReceivedGetKeys.Should().Equal(cacheKey);
        grantCache.ReceivedGetCancellationTokens.Should().Equal(cancellationToken);
        callTrace.Should().Equal(CacheGetCall);
    }

    /// <summary>
    /// 缓存拒绝时直接返回稳定拒绝结果且不读取授权存储
    /// </summary>
    /// <returns>权限检查完成后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_ReturnsDeniedWithoutReadingStore_WhenCacheDenies()
    {
        var context = CreateContext();
        var cacheKey = CreateCacheKey(context);
        var cancellationToken = TestContext.Current.CancellationToken;
        var callTrace = new List<string>();
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(true), callTrace);
        var grantCache = new RecordingPermissionGrantCache(
            callTrace,
            [new KeyValuePair<PermissionGrantCacheKey, bool>(cacheKey, false)]);
        var checker = new PermissionChecker(grantStore, grantCache);

        var result = await checker.CheckAsync(context, cancellationToken);

        result.Should().Be(AuthorizationResult.Denied("AUTHORIZATION:000001", "没有操作权限"));
        grantStore.CallCount.Should().Be(0);
        grantCache.GetCallCount.Should().Be(1);
        grantCache.SetCallCount.Should().Be(0);
        grantCache.ReceivedGetKeys.Should().Equal(cacheKey);
        grantCache.ReceivedGetCancellationTokens.Should().Equal(cancellationToken);
        callTrace.Should().Equal(CacheGetCall);
    }

    /// <summary>
    /// 首次缓存未命中时按读取、存储、写入顺序缓存授权，第二次直接命中缓存
    /// </summary>
    /// <returns>两次权限检查完成后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_CachesStoreGrantAndReusesIt_OnConsecutiveChecks()
    {
        var context = CreateContext();
        var cacheKey = CreateCacheKey(context);
        var cancellationToken = TestContext.Current.CancellationToken;
        var callTrace = new List<string>();
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(true), callTrace);
        var grantCache = new RecordingPermissionGrantCache(callTrace);
        var checker = new PermissionChecker(grantStore, grantCache);

        var firstResult = await checker.CheckAsync(context, cancellationToken);
        var secondResult = await checker.CheckAsync(context, cancellationToken);

        firstResult.Should().Be(AuthorizationResult.Success());
        secondResult.Should().Be(AuthorizationResult.Success());
        grantStore.CallCount.Should().Be(1);
        grantStore.ReceivedContext.Should().BeSameAs(context);
        grantStore.ReceivedCancellationToken.Should().Be(cancellationToken);
        grantCache.GetCallCount.Should().Be(2);
        grantCache.SetCallCount.Should().Be(1);
        grantCache.ReceivedGetKeys.Should().Equal(cacheKey, cacheKey);
        grantCache.ReceivedGetCancellationTokens.Should().Equal(cancellationToken, cancellationToken);
        grantCache.ReceivedSetKeys.Should().Equal(cacheKey);
        grantCache.ReceivedAllowedValues.Should().Equal(true);
        grantCache.ReceivedSetCancellationTokens.Should().Equal(cancellationToken);
        grantCache.CachedValues[cacheKey].Should().BeTrue();
        callTrace.Should().Equal(CacheGetCall, StoreHasGrantCall, CacheSetCall, CacheGetCall);
    }

    /// <summary>
    /// 缓存未命中且授权记录为空时返回拒绝并缓存拒绝结果
    /// </summary>
    /// <returns>权限检查完成后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_ReturnsDeniedAndCachesFalse_WhenGrantStoreIsEmpty()
    {
        var context = CreateContext();
        var cacheKey = CreateCacheKey(context);
        var cancellationToken = TestContext.Current.CancellationToken;
        var callTrace = new List<string>();
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(false), callTrace);
        var grantCache = new RecordingPermissionGrantCache(callTrace);
        var checker = new PermissionChecker(grantStore, grantCache);

        var result = await checker.CheckAsync(context, cancellationToken);

        result.Should().Be(AuthorizationResult.Denied("AUTHORIZATION:000001", "没有操作权限"));
        grantStore.CallCount.Should().Be(1);
        grantCache.GetCallCount.Should().Be(1);
        grantCache.SetCallCount.Should().Be(1);
        grantCache.ReceivedGetKeys.Should().Equal(cacheKey);
        grantCache.ReceivedSetKeys.Should().Equal(cacheKey);
        grantCache.ReceivedGetCancellationTokens.Should().Equal(cancellationToken);
        grantCache.ReceivedSetCancellationTokens.Should().Equal(cancellationToken);
        grantCache.CachedValues[cacheKey].Should().BeFalse();
        callTrace.Should().Equal(CacheGetCall, StoreHasGrantCall, CacheSetCall);
    }

    /// <summary>
    /// 缓存读取响应取消时向调用方传播原取消令牌且不访问后续依赖
    /// </summary>
    /// <returns>权限检查取消后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_PropagatesCancellation_WhenCacheReadIsCanceled()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellationToken = cancellationSource.Token;
        var callTrace = new List<string>();
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(true), callTrace);
        var grantCache = new RecordingPermissionGrantCache(
            (_, token) => Task.FromCanceled<bool?>(token),
            callTrace: callTrace);
        var checker = new PermissionChecker(grantStore, grantCache);

        Func<Task> act = () => checker.CheckAsync(CreateContext(), cancellationToken);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.CancellationToken.Should().Be(cancellationToken);
        grantStore.CallCount.Should().Be(0);
        grantCache.GetCallCount.Should().Be(1);
        grantCache.SetCallCount.Should().Be(0);
        grantCache.CachedValues.Should().BeEmpty();
        callTrace.Should().Equal(CacheGetCall);
    }

    /// <summary>
    /// 缓存读取失败时向调用方传播原始异常且不访问后续依赖
    /// </summary>
    /// <returns>权限检查失败后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_PropagatesFailure_WhenCacheReadFails()
    {
        var expectedException = new InvalidOperationException("授权缓存读取失败");
        var callTrace = new List<string>();
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(true), callTrace);
        var grantCache = new RecordingPermissionGrantCache(
            (_, _) => Task.FromException<bool?>(expectedException),
            callTrace: callTrace);
        var checker = new PermissionChecker(grantStore, grantCache);

        Func<Task> act = () => checker.CheckAsync(CreateContext(), TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
        grantStore.CallCount.Should().Be(0);
        grantCache.GetCallCount.Should().Be(1);
        grantCache.SetCallCount.Should().Be(0);
        grantCache.CachedValues.Should().BeEmpty();
        callTrace.Should().Equal(CacheGetCall);
    }

    /// <summary>
    /// 授权存储失败时向调用方传播原始异常且不写入缓存
    /// </summary>
    /// <returns>权限检查失败后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_PropagatesFailureWithoutCaching_WhenGrantStoreFails()
    {
        var expectedException = new InvalidOperationException("授权存储读取失败");
        var callTrace = new List<string>();
        var grantStore = new RecordingGrantStore(
            (_, _) => Task.FromException<bool>(expectedException),
            callTrace);
        var grantCache = new RecordingPermissionGrantCache(callTrace);
        var checker = new PermissionChecker(grantStore, grantCache);

        Func<Task> act = () => checker.CheckAsync(CreateContext(), TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
        grantStore.CallCount.Should().Be(1);
        grantCache.GetCallCount.Should().Be(1);
        grantCache.SetCallCount.Should().Be(0);
        grantCache.CachedValues.Should().BeEmpty();
        callTrace.Should().Equal(CacheGetCall, StoreHasGrantCall);
    }

    /// <summary>
    /// 授权存储响应取消时向调用方传播原取消令牌且不写入缓存
    /// </summary>
    /// <returns>权限检查取消后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_PropagatesCancellationWithoutCaching_WhenGrantStoreIsCanceled()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellationToken = cancellationSource.Token;
        var callTrace = new List<string>();
        var grantStore = new RecordingGrantStore(
            (_, token) => Task.FromCanceled<bool>(token),
            callTrace);
        var grantCache = new RecordingPermissionGrantCache(callTrace);
        var checker = new PermissionChecker(grantStore, grantCache);

        Func<Task> act = () => checker.CheckAsync(CreateContext(), cancellationToken);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.CancellationToken.Should().Be(cancellationToken);
        grantStore.CallCount.Should().Be(1);
        grantCache.GetCallCount.Should().Be(1);
        grantCache.SetCallCount.Should().Be(0);
        grantCache.CachedValues.Should().BeEmpty();
        callTrace.Should().Equal(CacheGetCall, StoreHasGrantCall);
    }

    /// <summary>
    /// 缓存写入失败时向调用方传播原始异常且不保存授权结果
    /// </summary>
    /// <returns>权限检查失败后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_PropagatesFailureWithoutCaching_WhenCacheWriteFails()
    {
        var expectedException = new InvalidOperationException("授权缓存写入失败");
        var callTrace = new List<string>();
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(true), callTrace);
        var grantCache = new RecordingPermissionGrantCache(
            (_, _) => Task.FromResult<bool?>(null),
            (_, _, _) => Task.FromException(expectedException),
            callTrace);
        var checker = new PermissionChecker(grantStore, grantCache);

        Func<Task> act = () => checker.CheckAsync(CreateContext(), TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
        grantStore.CallCount.Should().Be(1);
        grantCache.GetCallCount.Should().Be(1);
        grantCache.SetCallCount.Should().Be(1);
        grantCache.CachedValues.Should().BeEmpty();
        callTrace.Should().Equal(CacheGetCall, StoreHasGrantCall, CacheSetCall);
    }

    /// <summary>
    /// 缓存写入响应取消时向调用方传播原取消令牌且不保存授权结果
    /// </summary>
    /// <returns>权限检查取消后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_PropagatesCancellationWithoutCaching_WhenCacheWriteIsCanceled()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellationToken = cancellationSource.Token;
        var cacheKey = CreateCacheKey(CreateContext());
        var callTrace = new List<string>();
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(true), callTrace);
        var grantCache = new RecordingPermissionGrantCache(
            (_, _) => Task.FromResult<bool?>(null),
            (_, _, token) => Task.FromCanceled(token),
            callTrace);
        var checker = new PermissionChecker(grantStore, grantCache);

        Func<Task> act = () => checker.CheckAsync(CreateContext(), cancellationToken);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.CancellationToken.Should().Be(cancellationToken);
        grantStore.CallCount.Should().Be(1);
        grantCache.GetCallCount.Should().Be(1);
        grantCache.SetCallCount.Should().Be(1);
        grantCache.ReceivedGetKeys.Should().Equal(cacheKey);
        grantCache.ReceivedSetKeys.Should().Equal(cacheKey);
        grantCache.ReceivedGetCancellationTokens.Should().Equal(cancellationToken);
        grantCache.ReceivedSetCancellationTokens.Should().Equal(cancellationToken);
        grantCache.CachedValues.Should().BeEmpty();
        callTrace.Should().Equal(CacheGetCall, StoreHasGrantCall, CacheSetCall);
    }

    /// <summary>
    /// 创建包含租户、资源和角色信息的授权上下文
    /// </summary>
    /// <returns>用于权限检查的确定性上下文</returns>
    private static AuthorizationContext CreateContext()
    {
        return new AuthorizationContext(
            SubjectId: "user-1",
            TenantId: "tenant-1",
            Permission: "orders.approve",
            ResourceType: "Order",
            ResourceId: "order-1",
            Roles: new HashSet<string>(StringComparer.Ordinal) { "cashier" });
    }

    /// <summary>
    /// 从授权上下文创建包含全部稳定隔离字段的缓存键
    /// </summary>
    /// <param name="context">需要转换为缓存键的授权上下文</param>
    /// <returns>与权限检查器构造规则一致的完整缓存键</returns>
    private static PermissionGrantCacheKey CreateCacheKey(AuthorizationContext context)
    {
        return new PermissionGrantCacheKey(
            context.SubjectId,
            context.TenantId,
            context.Permission,
            context.ResourceType,
            context.ResourceId);
    }

    /// <summary>
    /// 记录授权存储读取参数并返回测试指定结果
    /// </summary>
    private sealed class RecordingGrantStore : IGrantStore
    {
        /// <summary>
        /// 生成授权记录读取结果的测试委托
        /// </summary>
        private readonly Func<AuthorizationContext, CancellationToken, Task<bool>> _readGrantAsync;

        /// <summary>
        /// 与缓存替身共享的依赖调用轨迹
        /// </summary>
        private readonly IList<string> _callTrace;

        /// <summary>
        /// 初始化可记录调用的授权存储替身
        /// </summary>
        /// <param name="readGrantAsync">根据调用参数生成授权记录读取结果的委托</param>
        /// <param name="callTrace">与缓存替身共享的依赖调用轨迹</param>
        public RecordingGrantStore(
            Func<AuthorizationContext, CancellationToken, Task<bool>> readGrantAsync,
            IList<string>? callTrace = null)
        {
            _readGrantAsync = readGrantAsync;
            _callTrace = callTrace ?? new List<string>();
        }

        /// <summary>
        /// 授权存储被读取的次数
        /// </summary>
        public int CallCount { get; private set; }

        /// <summary>
        /// 最近一次读取携带的授权上下文
        /// </summary>
        public AuthorizationContext? ReceivedContext { get; private set; }

        /// <summary>
        /// 最近一次读取携带的取消令牌
        /// </summary>
        public CancellationToken ReceivedCancellationToken { get; private set; }

        /// <inheritdoc />
        public Task<bool> HasGrantAsync(AuthorizationContext context, CancellationToken cancellationToken)
        {
            CallCount++;
            ReceivedContext = context;
            ReceivedCancellationToken = cancellationToken;
            _callTrace.Add(StoreHasGrantCall);
            return _readGrantAsync(context, cancellationToken);
        }
    }

    /// <summary>
    /// 保存缓存状态并记录全部读写参数与共享调用轨迹
    /// </summary>
    private sealed class RecordingPermissionGrantCache : IPermissionGrantCache
    {
        /// <summary>
        /// 缓存成功写入的授权判断
        /// </summary>
        private readonly Dictionary<PermissionGrantCacheKey, bool> _values;

        /// <summary>
        /// 覆盖缓存读取结果的测试委托
        /// </summary>
        private readonly Func<PermissionGrantCacheKey, CancellationToken, Task<bool?>>? _readAsync;

        /// <summary>
        /// 覆盖缓存写入结果的测试委托
        /// </summary>
        private readonly Func<PermissionGrantCacheKey, bool, CancellationToken, Task>? _writeAsync;

        /// <summary>
        /// 与授权存储替身共享的依赖调用轨迹
        /// </summary>
        private readonly IList<string> _callTrace;

        /// <summary>
        /// 缓存读取接收的完整键序列
        /// </summary>
        private readonly List<PermissionGrantCacheKey> _receivedGetKeys = [];

        /// <summary>
        /// 缓存读取接收的取消令牌序列
        /// </summary>
        private readonly List<CancellationToken> _receivedGetCancellationTokens = [];

        /// <summary>
        /// 缓存写入接收的完整键序列
        /// </summary>
        private readonly List<PermissionGrantCacheKey> _receivedSetKeys = [];

        /// <summary>
        /// 缓存写入接收的授权判断序列
        /// </summary>
        private readonly List<bool> _receivedAllowedValues = [];

        /// <summary>
        /// 缓存写入接收的取消令牌序列
        /// </summary>
        private readonly List<CancellationToken> _receivedSetCancellationTokens = [];

        /// <summary>
        /// 初始化维护真实内存状态并记录调用的授权缓存替身
        /// </summary>
        /// <param name="callTrace">与授权存储替身共享的依赖调用轨迹</param>
        /// <param name="values">缓存初始化时已经存在的授权判断</param>
        public RecordingPermissionGrantCache(
            IList<string> callTrace,
            IEnumerable<KeyValuePair<PermissionGrantCacheKey, bool>>? values = null)
        {
            _callTrace = callTrace;
            _values = values?.ToDictionary() ?? [];
        }

        /// <summary>
        /// 初始化可覆盖读写失败并记录调用的授权缓存替身
        /// </summary>
        /// <param name="readAsync">根据调用参数生成缓存读取结果的委托</param>
        /// <param name="writeAsync">根据调用参数生成缓存写入结果的委托</param>
        /// <param name="callTrace">与授权存储替身共享的依赖调用轨迹</param>
        public RecordingPermissionGrantCache(
            Func<PermissionGrantCacheKey, CancellationToken, Task<bool?>> readAsync,
            Func<PermissionGrantCacheKey, bool, CancellationToken, Task>? writeAsync = null,
            IList<string>? callTrace = null)
        {
            _readAsync = readAsync;
            _writeAsync = writeAsync;
            _callTrace = callTrace ?? new List<string>();
            _values = [];
        }

        /// <summary>
        /// 授权缓存被读取的次数
        /// </summary>
        public int GetCallCount { get; private set; }

        /// <summary>
        /// 授权缓存被写入的次数
        /// </summary>
        public int SetCallCount { get; private set; }

        /// <summary>
        /// 缓存读取接收的完整键序列
        /// </summary>
        public IReadOnlyList<PermissionGrantCacheKey> ReceivedGetKeys => _receivedGetKeys;

        /// <summary>
        /// 缓存读取接收的取消令牌序列
        /// </summary>
        public IReadOnlyList<CancellationToken> ReceivedGetCancellationTokens => _receivedGetCancellationTokens;

        /// <summary>
        /// 缓存写入接收的完整键序列
        /// </summary>
        public IReadOnlyList<PermissionGrantCacheKey> ReceivedSetKeys => _receivedSetKeys;

        /// <summary>
        /// 缓存写入接收的授权判断序列
        /// </summary>
        public IReadOnlyList<bool> ReceivedAllowedValues => _receivedAllowedValues;

        /// <summary>
        /// 缓存写入接收的取消令牌序列
        /// </summary>
        public IReadOnlyList<CancellationToken> ReceivedSetCancellationTokens => _receivedSetCancellationTokens;

        /// <summary>
        /// 已成功写入缓存的授权判断只读视图
        /// </summary>
        public IReadOnlyDictionary<PermissionGrantCacheKey, bool> CachedValues => _values;

        /// <inheritdoc />
        public Task<bool?> GetAsync(PermissionGrantCacheKey key, CancellationToken cancellationToken)
        {
            GetCallCount++;
            _receivedGetKeys.Add(key);
            _receivedGetCancellationTokens.Add(cancellationToken);
            _callTrace.Add(CacheGetCall);

            return _readAsync is not null
                ? _readAsync(key, cancellationToken)
                : Task.FromResult(_values.TryGetValue(key, out var allowed) ? allowed : (bool?)null);
        }

        /// <inheritdoc />
        public async Task SetAsync(
            PermissionGrantCacheKey key,
            bool allowed,
            CancellationToken cancellationToken)
        {
            SetCallCount++;
            _receivedSetKeys.Add(key);
            _receivedAllowedValues.Add(allowed);
            _receivedSetCancellationTokens.Add(cancellationToken);
            _callTrace.Add(CacheSetCall);

            if (_writeAsync is not null)
            {
                await _writeAsync(key, allowed, cancellationToken);
            }

            _values[key] = allowed;
        }
    }
}
