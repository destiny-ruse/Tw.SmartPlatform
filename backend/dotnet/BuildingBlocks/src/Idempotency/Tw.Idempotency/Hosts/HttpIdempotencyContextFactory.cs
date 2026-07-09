namespace Tw.Idempotency.Hosts;

/// <summary>表示 HttpIdempotencyContextFactory 类型</summary>
public static class HttpIdempotencyContextFactory
{
    /// <summary>执行 Create 操作</summary>
    /// <param name="tenantId">tenantId 参数</param>
    /// <param name="resourceType">resourceType 参数</param>
    /// <param name="operation">operation 参数</param>
    /// <param name="idempotencyHeader">idempotencyHeader 参数</param>
    /// <returns>Create 的执行结果</returns>
    public static IdempotencyKey Create(string tenantId, string resourceType, string operation, string idempotencyHeader)
    {
        Validate(tenantId, resourceType, operation, idempotencyHeader);
        return new IdempotencyKey(IdempotencyBoundary.Http, tenantId, resourceType, operation, idempotencyHeader);
    }

    /// <summary>执行 Validate 操作</summary>
    /// <param name="values">values 参数</param>
    internal static void Validate(params string[] values)
    {
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        }
    }
}
