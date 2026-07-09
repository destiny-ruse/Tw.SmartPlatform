namespace Tw.Data.Concurrency;

public interface IConcurrencyCheckContext
{
    string ResourceType { get; }

    string ResourceId { get; }

    string? ExpectedConcurrencyStamp { get; }

    long? ExpectedVersionStamp { get; }
}
