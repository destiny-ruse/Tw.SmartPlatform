namespace Tw.Uow;

public interface IOutboxTransactionBoundary
{
    bool CanWriteOutbox { get; }

    bool IsCompleted { get; }
}
