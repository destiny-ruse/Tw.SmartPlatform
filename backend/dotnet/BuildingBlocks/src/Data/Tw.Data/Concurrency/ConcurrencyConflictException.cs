namespace Tw.Data.Concurrency;

public sealed class ConcurrencyConflictException(string resourceType, string resourceId)
    : Exception("Data has been changed by another request.")
{
    public string Code { get; } = "DATA:CONFLICT";

    public string ResourceType { get; } = resourceType;

    public string ResourceId { get; } = resourceId;
}
