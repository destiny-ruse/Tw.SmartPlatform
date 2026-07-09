namespace Tw.Resilience;

public enum OperationKind
{
    Read = 1,
    IdempotentWrite = 2,
    NonIdempotentWrite = 3
}
