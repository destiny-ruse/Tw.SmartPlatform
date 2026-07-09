using AwesomeAssertions;
using Tw.Idempotency;
using Xunit;

namespace Tw.Idempotency.Tests;

public sealed class IdempotencyExecutorTests
{
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

    private sealed class InMemoryIdempotencyStore : IIdempotencyStore
    {
        private readonly Dictionary<IdempotencyKey, (string Fingerprint, IdempotencyResult? Result)> _entries = [];

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

        public Task<IdempotencyResult?> GetAsync(IdempotencyKey key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_entries.TryGetValue(key, out var entry) ? entry.Result : null);
        }

        public Task CompleteAsync(IdempotencyKey key, IdempotencyResult result, CancellationToken cancellationToken = default)
        {
            var entry = _entries[key];
            _entries[key] = (entry.Fingerprint, result);
            return Task.CompletedTask;
        }
    }
}
