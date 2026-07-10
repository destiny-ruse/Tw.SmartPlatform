namespace Tw.Idempotency.Hosts;

/// <summary>
/// 根据 gRPC metadata 创建幂等键上下文
/// </summary>
public static class GrpcIdempotencyContextFactory
{
    /// <summary>
    /// 创建统一 API 错误响应对象
    /// </summary>
    /// <param name="tenantId">用于提供tenant标识</param>
    /// <param name="resourceType">用于提供resource类型</param>
    /// <param name="operation">需要在幂等保护下运行的业务委托</param>
    /// <param name="metadataKey">用于提供metadata键</param>
    /// <returns>方法计算得到的文本值</returns>
    public static IdempotencyKey Create(string tenantId, string resourceType, string operation, string metadataKey)
    {
        HttpIdempotencyContextFactory.Validate(tenantId, resourceType, operation, metadataKey);
        return new IdempotencyKey(IdempotencyBoundary.Grpc, tenantId, resourceType, operation, metadataKey);
    }
}
