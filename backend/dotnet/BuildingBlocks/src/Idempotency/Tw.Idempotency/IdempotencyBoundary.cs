namespace Tw.Idempotency;

public enum IdempotencyBoundary
{
    Http = 1,
    Grpc = 2,
    Cap = 3,
    BackgroundJob = 4
}
