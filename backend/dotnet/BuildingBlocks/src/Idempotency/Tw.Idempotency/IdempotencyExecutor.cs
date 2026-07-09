namespace Tw.Idempotency;

public sealed class IdempotencyExecutor(IIdempotencyStore store)
{
    public async Task<IdempotencyResult> ExecuteAsync(
        IdempotencyKey key,
        string fingerprint,
        Func<Task<IdempotencyResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);
        ArgumentNullException.ThrowIfNull(operation);

        var reservation = await store.TryBeginAsync(key, fingerprint, cancellationToken);
        if (reservation.Status == IdempotencyReservationStatus.Duplicate)
        {
            return reservation.ExistingResult
                ?? await store.GetAsync(key, cancellationToken)
                ?? IdempotencyResult.Conflict("IDEMPOTENCY:000409");
        }

        if (reservation.Status == IdempotencyReservationStatus.Conflict)
        {
            throw new IdempotencyConflictException(key);
        }

        var result = await operation();
        await store.CompleteAsync(key, result, cancellationToken);
        return result;
    }
}
