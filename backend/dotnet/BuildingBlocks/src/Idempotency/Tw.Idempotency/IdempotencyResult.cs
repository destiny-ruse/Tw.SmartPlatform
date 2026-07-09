namespace Tw.Idempotency;

public sealed record IdempotencyResult(int StatusCode, string Body, string Code)
{
    public static IdempotencyResult Success(int statusCode, string body) => new(statusCode, body, "SYSTEM:000000");

    public static IdempotencyResult Conflict(string code) => new(409, string.Empty, code);
}
