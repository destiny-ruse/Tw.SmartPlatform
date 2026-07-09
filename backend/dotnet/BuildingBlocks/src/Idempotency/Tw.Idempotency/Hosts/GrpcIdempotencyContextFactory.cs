namespace Tw.Idempotency.Hosts;

public static class GrpcIdempotencyContextFactory
{
    public static IdempotencyKey Create(string tenantId, string resourceType, string operation, string metadataKey)
    {
        HttpIdempotencyContextFactory.Validate(tenantId, resourceType, operation, metadataKey);
        return new IdempotencyKey(IdempotencyBoundary.Grpc, tenantId, resourceType, operation, metadataKey);
    }
}
