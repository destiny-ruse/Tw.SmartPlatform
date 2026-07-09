namespace Tw.Idempotency;

public interface IIdempotencyStore
{
    Task<IdempotencyReservation> TryBeginAsync(IdempotencyKey key, string fingerprint, CancellationToken cancellationToken = default);

    Task<IdempotencyResult?> GetAsync(IdempotencyKey key, CancellationToken cancellationToken = default);

    Task CompleteAsync(IdempotencyKey key, IdempotencyResult result, CancellationToken cancellationToken = default);
}
