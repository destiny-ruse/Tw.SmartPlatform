using AwesomeAssertions;
using Tw.Idempotency;
using Xunit;

namespace Tw.Idempotency.Tests;

/// <summary>
/// 覆盖幂等执行器的核心行为和边界条件
/// </summary>
public sealed class IdempotencyExecutorTests
{
    /// <summary>
    /// 验证执行异步返回第一个结果针对重复请求
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task ExecuteAsync_ReturnsFirstResultForDuplicateRequest()
    {
        var store = new InMemoryIdempotencyStore();
        var executor = new IdempotencyExecutor(store);
        var key = new IdempotencyKey(IdempotencyBoundary.Http, "tenant-a", "Order", "Create", "request-1");

        var first = await executor.ExecuteAsync(
            key,
            "body-hash-1",
            () => Task.FromResult(IdempotencyResult.Success(201, "created")),
            TestContext.Current.CancellationToken);
        var duplicate = await executor.ExecuteAsync(
            key,
            "body-hash-1",
            () => Task.FromResult(IdempotencyResult.Success(201, "duplicate-created")),
            TestContext.Current.CancellationToken);

        first.Should().Be(IdempotencyResult.Success(201, "created"));
        duplicate.Should().Be(IdempotencyResult.Success(201, "created"));
    }

    /// <summary>
    /// 验证执行异步抛出异常StableConflict代码当Same键HasDifferent指纹
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task ExecuteAsync_ThrowsStableConflictCode_WhenSameKeyHasDifferentFingerprint()
    {
        var store = new InMemoryIdempotencyStore();
        var executor = new IdempotencyExecutor(store);
        var key = new IdempotencyKey(IdempotencyBoundary.Http, "tenant-a", "Order", "Create", "request-1");

        await executor.ExecuteAsync(
            key,
            "body-hash-1",
            () => Task.FromResult(IdempotencyResult.Success(201, "created")),
            TestContext.Current.CancellationToken);

        var act = () => executor.ExecuteAsync(
            key,
            "body-hash-2",
            () => Task.FromResult(IdempotencyResult.Success(201, "created")),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<IdempotencyConflictException>()
            .Where(exception => exception.Code == "IDEMPOTENCY:000409");
    }

    /// <summary>
    /// 覆盖内存幂等存储的核心行为和边界条件
    /// </summary>
    private sealed class InMemoryIdempotencyStore : IIdempotencyStore
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的entries
        /// </summary>
        private readonly Dictionary<IdempotencyKey, (string Fingerprint, IdempotencyResult? Result)> _entries = [];

        /// <summary>
        /// 尝试开始幂等请求处理并返回占用状态
        /// </summary>
        /// <param name="key">用于定位目标数据或缓存项的键</param>
        /// <param name="fingerprint">用于区分幂等请求负载的指纹</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的幂等占用结果</returns>
        public Task<IdempotencyReservation> TryBeginAsync(IdempotencyKey key, string fingerprint, CancellationToken cancellationToken = default)
        {
            if (!_entries.TryGetValue(key, out var entry))
            {
                _entries[key] = (fingerprint, null);
                return Task.FromResult(new IdempotencyReservation(IdempotencyReservationStatus.Started, null));
            }

            if (!string.Equals(entry.Fingerprint, fingerprint, StringComparison.Ordinal))
            {
                return Task.FromResult(new IdempotencyReservation(IdempotencyReservationStatus.Conflict, null));
            }

            return Task.FromResult(new IdempotencyReservation(IdempotencyReservationStatus.Duplicate, entry.Result));
        }

        /// <summary>
        /// 从测试替身中读取指定条目
        /// </summary>
        /// <param name="key">用于定位目标数据或缓存项的键</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的幂等处理结果</returns>
        public Task<IdempotencyResult?> GetAsync(IdempotencyKey key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_entries.TryGetValue(key, out var entry) ? entry.Result : null);
        }

        /// <summary>
        /// 将幂等请求标记为完成并保存结果
        /// </summary>
        /// <param name="key">用于定位目标数据或缓存项的键</param>
        /// <param name="result">当前流程预置或返回的结果</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task CompleteAsync(IdempotencyKey key, IdempotencyResult result, CancellationToken cancellationToken = default)
        {
            var entry = _entries[key];
            _entries[key] = (entry.Fingerprint, result);
            return Task.CompletedTask;
        }
    }
}
