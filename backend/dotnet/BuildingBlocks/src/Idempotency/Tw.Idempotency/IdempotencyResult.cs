namespace Tw.Idempotency;

/// <summary>表示 IdempotencyResult 声明</summary>
public sealed record IdempotencyResult(int StatusCode, string Body, string Code)
{
    /// <summary>执行 Success 操作</summary>
    /// <param name="statusCode">statusCode 参数</param>
    /// <param name="body">body 参数</param>
    /// <returns>Success 的执行结果</returns>
    public static IdempotencyResult Success(int statusCode, string body) => new(statusCode, body, "SYSTEM:000000");

    /// <summary>执行 Conflict 操作</summary>
    /// <param name="code">code 参数</param>
    /// <returns>Conflict 的执行结果</returns>
    public static IdempotencyResult Conflict(string code) => new(409, string.Empty, code);
}
