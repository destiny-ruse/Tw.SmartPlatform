namespace Tw.Idempotency.Hosts;

/// <summary>
/// 根据 HTTP 请求头创建幂等键上下文
/// </summary>
public static class HttpIdempotencyContextFactory
{
    /// <summary>
    /// 创建统一 API 错误响应对象
    /// </summary>
    /// <param name="tenantId">用于提供tenant标识</param>
    /// <param name="resourceType">用于提供resource类型</param>
    /// <param name="operation">需要在幂等保护下运行的业务委托</param>
    /// <param name="idempotencyHeader">HTTP Idempotency-Key 请求头值</param>
    /// <returns>方法计算得到的文本值</returns>
    public static IdempotencyKey Create(string tenantId, string resourceType, string operation, string idempotencyHeader)
    {
        Validate(tenantId, resourceType, operation, idempotencyHeader);
        return new IdempotencyKey(IdempotencyBoundary.Http, tenantId, resourceType, operation, idempotencyHeader);
    }

    /// <summary>
    /// 校验当前配置或输入约束，并在非法时抛出异常
    /// </summary>
    /// <param name="values">用于提供values</param>
    internal static void Validate(params string[] values)
    {
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        }
    }
}
