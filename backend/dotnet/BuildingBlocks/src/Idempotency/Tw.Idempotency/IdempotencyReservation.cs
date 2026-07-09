namespace Tw.Idempotency;

public enum IdempotencyReservationStatus
{
    Started = 1,
    Duplicate = 2,
    Conflict = 3
}

public sealed record IdempotencyReservation(IdempotencyReservationStatus Status, IdempotencyResult? ExistingResult);
