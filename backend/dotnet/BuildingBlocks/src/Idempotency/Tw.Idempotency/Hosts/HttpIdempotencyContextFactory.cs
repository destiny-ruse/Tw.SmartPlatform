namespace Tw.Idempotency.Hosts;

public static class HttpIdempotencyContextFactory
{
    public static IdempotencyKey Create(string tenantId, string resourceType, string operation, string idempotencyHeader)
    {
        Validate(tenantId, resourceType, operation, idempotencyHeader);
        return new IdempotencyKey(IdempotencyBoundary.Http, tenantId, resourceType, operation, idempotencyHeader);
    }

    internal static void Validate(params string[] values)
    {
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        }
    }
}
