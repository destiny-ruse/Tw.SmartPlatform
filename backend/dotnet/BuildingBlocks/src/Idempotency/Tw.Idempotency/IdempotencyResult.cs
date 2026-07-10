namespace Tw.Idempotency;

/// <summary>
/// 承载幂等请求处理后的结果数据
/// </summary>
public sealed record IdempotencyResult(int StatusCode, string Body, string Code)
{
    /// <summary>
    /// 创建幂等请求成功结果
    /// </summary>
    /// <param name="statusCode">HTTP 响应状态码</param>
    /// <param name="body">需要缓存或返回的响应正文</param>
    /// <returns>幂等请求处理结果</returns>
    public static IdempotencyResult Success(int statusCode, string body) => new(statusCode, body, "SYSTEM:000000");

    /// <summary>
    /// 创建幂等请求冲突结果
    /// </summary>
    /// <param name="code">对外返回的稳定错误码</param>
    /// <returns>幂等请求处理结果</returns>
    public static IdempotencyResult Conflict(string code) => new(409, string.Empty, code);
}
