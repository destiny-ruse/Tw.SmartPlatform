using AwesomeAssertions;
using Tw.Idempotency;
using Xunit;

namespace Tw.Idempotency.Tests;

/// <summary>验证 IdempotencyExecutorTests 相关行为</summary>
public sealed class IdempotencyExecutorTests
{
    /// <summary>验证 ExecuteAsync_ReturnsFirstResultForDuplicateRequest 场景</summary>
    /// <returns>ExecuteAsync_ReturnsFirstResultForDuplicateRequest 的执行结果</returns>
    [Fact]
    public async Task ExecuteAsync_ReturnsFirstResultForDuplicateRequest()
    {
        var store = new InMemoryIdempotencyStore();
        var executor = new IdempotencyExecutor(store);
        var key = new IdempotencyKey(IdempotencyBoundary.Http, "tenant-a", "Order", "Create", "request-1");

        var first = await executor.ExecuteAsync(key, "body-hash-1", () => Task.FromResult(IdempotencyResult.Success(201, "created")));
        var duplicate = await executor.ExecuteAsync(key, "body-hash-1", () => Task.FromResult(IdempotencyResult.Success(201, "duplicate-created")));

        first.Should().Be(IdempotencyResult.Success(201, "created"));
        duplicate.Should().Be(IdempotencyResult.Success(201, "created"));
    }

    /// <summary>验证 ExecuteAsync_ThrowsStableConflictCode_WhenSameKeyHasDifferentFingerprint 场景</summary>
    /// <returns>ExecuteAsync_ThrowsStableConflictCode_WhenSameKeyHasDifferentFingerprint 的执行结果</returns>
    [Fact]
    public async Task ExecuteAsync_ThrowsStableConflictCode_WhenSameKeyHasDifferentFingerprint()
    {
        var store = new InMemoryIdempotencyStore();
        var executor = new IdempotencyExecutor(store);
        var key = new IdempotencyKey(IdempotencyBoundary.Http, "tenant-a", "Order", "Create", "request-1");

        await executor.ExecuteAsync(key, "body-hash-1", () => Task.FromResult(IdempotencyResult.Success(201, "created")));

        var act = () => executor.ExecuteAsync(key, "body-hash-2", () => Task.FromResult(IdempotencyResult.Success(201, "created")));

        await act.Should().ThrowAsync<IdempotencyConflictException>()
            .Where(exception => exception.Code == "IDEMPOTENCY:000409");
    }

    /// <summary>验证 InMemoryIdempotencyStore 相关行为</summary>
    private sealed class InMemoryIdempotencyStore : IIdempotencyStore
    {
        /// <summary>表示 _entries 字段</summary>
        private readonly Dictionary<IdempotencyKey, (string Fingerprint, IdempotencyResult? Result)> _entries = [];

        /// <summary>验证 TryBeginAsync 场景</summary>
        /// <param name="key">key 参数</param>
        /// <param name="fingerprint">fingerprint 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>TryBeginAsync 的执行结果</returns>
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

        /// <summary>验证 GetAsync 场景</summary>
        /// <param name="key">key 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>GetAsync 的执行结果</returns>
        public Task<IdempotencyResult?> GetAsync(IdempotencyKey key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_entries.TryGetValue(key, out var entry) ? entry.Result : null);
        }

        /// <summary>验证 CompleteAsync 场景</summary>
        /// <param name="key">key 参数</param>
        /// <param name="result">result 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>CompleteAsync 的执行结果</returns>
        public Task CompleteAsync(IdempotencyKey key, IdempotencyResult result, CancellationToken cancellationToken = default)
        {
            var entry = _entries[key];
            _entries[key] = (entry.Fingerprint, result);
            return Task.CompletedTask;
        }
    }
}
