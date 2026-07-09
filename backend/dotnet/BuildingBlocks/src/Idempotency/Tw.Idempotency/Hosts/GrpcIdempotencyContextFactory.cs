namespace Tw.Idempotency.Hosts;

/// <summary>表示 GrpcIdempotencyContextFactory 类型</summary>
public static class GrpcIdempotencyContextFactory
{
    /// <summary>执行 Create 操作</summary>
    /// <param name="tenantId">tenantId 参数</param>
    /// <param name="resourceType">resourceType 参数</param>
    /// <param name="operation">operation 参数</param>
    /// <param name="metadataKey">metadataKey 参数</param>
    /// <returns>Create 的执行结果</returns>
    public static IdempotencyKey Create(string tenantId, string resourceType, string operation, string metadataKey)
    {
        HttpIdempotencyContextFactory.Validate(tenantId, resourceType, operation, metadataKey);
        return new IdempotencyKey(IdempotencyBoundary.Grpc, tenantId, resourceType, operation, metadataKey);
    }
}
