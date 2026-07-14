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
    /// 缓存允许时直接返回成功且不读取授权存储
    /// </summary>
    /// <returns>权限检查完成后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_ReturnsSuccessWithoutReadingStore_WhenCacheAllows()
    {
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(false));
        var grantCache = new RecordingPermissionGrantCache((_, _) => Task.FromResult<bool?>(true));
        var checker = new PermissionChecker(grantStore, grantCache);

        var result = await checker.CheckAsync(CreateContext(), TestContext.Current.CancellationToken);

        result.Should().Be(AuthorizationResult.Success());
        grantStore.CallCount.Should().Be(0);
        grantCache.SetCallCount.Should().Be(0);
    }

    /// <summary>
    /// 缓存拒绝时直接返回稳定拒绝结果且不读取授权存储
    /// </summary>
    /// <returns>权限检查完成后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_ReturnsDeniedWithoutReadingStore_WhenCacheDenies()
    {
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(true));
        var grantCache = new RecordingPermissionGrantCache((_, _) => Task.FromResult<bool?>(false));
        var checker = new PermissionChecker(grantStore, grantCache);

        var result = await checker.CheckAsync(CreateContext(), TestContext.Current.CancellationToken);

        result.Should().Be(AuthorizationResult.Denied("AUTHORIZATION:000001", "没有操作权限"));
        grantStore.CallCount.Should().Be(0);
        grantCache.SetCallCount.Should().Be(0);
    }

    /// <summary>
    /// 缓存未命中且存在授权记录时读取存储并缓存允许结果
    /// </summary>
    /// <returns>权限检查完成后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_ReadsStoreAndCachesAllowedGrant_WhenCacheMisses()
    {
        var context = CreateContext();
        var cancellationToken = TestContext.Current.CancellationToken;
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(true));
        var grantCache = new RecordingPermissionGrantCache((_, _) => Task.FromResult<bool?>(null));
        var checker = new PermissionChecker(grantStore, grantCache);

        var result = await checker.CheckAsync(context, cancellationToken);

        result.Should().Be(AuthorizationResult.Success());
        grantStore.CallCount.Should().Be(1);
        grantStore.ReceivedContext.Should().BeSameAs(context);
        grantStore.ReceivedCancellationToken.Should().Be(cancellationToken);
        grantCache.SetCallCount.Should().Be(1);
        grantCache.ReceivedSetKey.Should().Be(new PermissionGrantCacheKey(
            context.SubjectId,
            context.TenantId,
            context.Permission,
            context.ResourceType,
            context.ResourceId));
        grantCache.ReceivedAllowed.Should().BeTrue();
        grantCache.ReceivedSetCancellationToken.Should().Be(cancellationToken);
    }

    /// <summary>
    /// 缓存未命中且授权记录为空时返回拒绝并缓存拒绝结果
    /// </summary>
    /// <returns>权限检查完成后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_ReturnsDeniedAndCachesFalse_WhenGrantStoreIsEmpty()
    {
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(false));
        var grantCache = new RecordingPermissionGrantCache((_, _) => Task.FromResult<bool?>(null));
        var checker = new PermissionChecker(grantStore, grantCache);

        var result = await checker.CheckAsync(CreateContext(), TestContext.Current.CancellationToken);

        result.Should().Be(AuthorizationResult.Denied("AUTHORIZATION:000001", "没有操作权限"));
        grantStore.CallCount.Should().Be(1);
        grantCache.SetCallCount.Should().Be(1);
        grantCache.ReceivedAllowed.Should().BeFalse();
    }

    /// <summary>
    /// 缓存读取响应取消时向调用方传播取消且不访问授权存储
    /// </summary>
    /// <returns>权限检查取消后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_PropagatesCancellation_WhenCacheReadIsCanceled()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellationToken = cancellationSource.Token;
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(true));
        var grantCache = new RecordingPermissionGrantCache(
            (_, token) => Task.FromCanceled<bool?>(token));
        var checker = new PermissionChecker(grantStore, grantCache);

        Func<Task> act = () => checker.CheckAsync(CreateContext(), cancellationToken);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.CancellationToken.Should().Be(cancellationToken);
        grantStore.CallCount.Should().Be(0);
        grantCache.SetCallCount.Should().Be(0);
    }

    /// <summary>
    /// 授权存储失败时向调用方传播原始异常且不写入缓存
    /// </summary>
    /// <returns>权限检查失败后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_PropagatesFailureWithoutCaching_WhenGrantStoreFails()
    {
        var expectedException = new InvalidOperationException("授权存储读取失败");
        var grantStore = new RecordingGrantStore(
            (_, _) => Task.FromException<bool>(expectedException));
        var grantCache = new RecordingPermissionGrantCache((_, _) => Task.FromResult<bool?>(null));
        var checker = new PermissionChecker(grantStore, grantCache);

        Func<Task> act = () => checker.CheckAsync(CreateContext(), TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
        grantCache.SetCallCount.Should().Be(0);
    }

    /// <summary>
    /// 缓存写入失败时向调用方传播原始异常
    /// </summary>
    /// <returns>权限检查失败后的异步任务</returns>
    [Fact]
    public async Task CheckAsync_PropagatesFailure_WhenCacheWriteFails()
    {
        var expectedException = new InvalidOperationException("授权缓存写入失败");
        var grantStore = new RecordingGrantStore((_, _) => Task.FromResult(true));
        var grantCache = new RecordingPermissionGrantCache(
            (_, _) => Task.FromResult<bool?>(null),
            (_, _, _) => Task.FromException(expectedException));
        var checker = new PermissionChecker(grantStore, grantCache);

        Func<Task> act = () => checker.CheckAsync(CreateContext(), TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
        grantStore.CallCount.Should().Be(1);
        grantCache.SetCallCount.Should().Be(1);
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
    /// 记录授权存储读取参数并返回测试指定结果
    /// </summary>
    private sealed class RecordingGrantStore : IGrantStore
    {
        /// <summary>
        /// 生成授权记录读取结果的测试委托
        /// </summary>
        private readonly Func<AuthorizationContext, CancellationToken, Task<bool>> _readGrantAsync;

        /// <summary>
        /// 初始化可记录调用的授权存储替身
        /// </summary>
        /// <param name="readGrantAsync">根据调用参数生成授权记录读取结果的委托</param>
        public RecordingGrantStore(Func<AuthorizationContext, CancellationToken, Task<bool>> readGrantAsync)
        {
            _readGrantAsync = readGrantAsync;
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
            return _readGrantAsync(context, cancellationToken);
        }
    }

    /// <summary>
    /// 记录授权缓存读写参数并返回测试指定结果
    /// </summary>
    private sealed class RecordingPermissionGrantCache : IPermissionGrantCache
    {
        /// <summary>
        /// 生成缓存读取结果的测试委托
        /// </summary>
        private readonly Func<PermissionGrantCacheKey, CancellationToken, Task<bool?>> _readAsync;

        /// <summary>
        /// 生成缓存写入结果的测试委托
        /// </summary>
        private readonly Func<PermissionGrantCacheKey, bool, CancellationToken, Task> _writeAsync;

        /// <summary>
        /// 初始化可记录调用的授权缓存替身
        /// </summary>
        /// <param name="readAsync">根据调用参数生成缓存读取结果的委托</param>
        /// <param name="writeAsync">根据调用参数生成缓存写入结果的委托</param>
        public RecordingPermissionGrantCache(
            Func<PermissionGrantCacheKey, CancellationToken, Task<bool?>> readAsync,
            Func<PermissionGrantCacheKey, bool, CancellationToken, Task>? writeAsync = null)
        {
            _readAsync = readAsync;
            _writeAsync = writeAsync ?? ((_, _, _) => Task.CompletedTask);
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
        /// 最近一次写入使用的缓存键
        /// </summary>
        public PermissionGrantCacheKey? ReceivedSetKey { get; private set; }

        /// <summary>
        /// 最近一次写入保存的授权判断
        /// </summary>
        public bool? ReceivedAllowed { get; private set; }

        /// <summary>
        /// 最近一次写入携带的取消令牌
        /// </summary>
        public CancellationToken ReceivedSetCancellationToken { get; private set; }

        /// <inheritdoc />
        public Task<bool?> GetAsync(PermissionGrantCacheKey key, CancellationToken cancellationToken)
        {
            GetCallCount++;
            return _readAsync(key, cancellationToken);
        }

        /// <inheritdoc />
        public Task SetAsync(
            PermissionGrantCacheKey key,
            bool allowed,
            CancellationToken cancellationToken)
        {
            SetCallCount++;
            ReceivedSetKey = key;
            ReceivedAllowed = allowed;
            ReceivedSetCancellationToken = cancellationToken;
            return _writeAsync(key, allowed, cancellationToken);
        }
    }
}
