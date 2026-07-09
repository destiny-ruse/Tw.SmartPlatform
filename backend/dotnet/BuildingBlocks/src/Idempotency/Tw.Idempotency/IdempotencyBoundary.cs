namespace Tw.Idempotency;

/// <summary>定义 IdempotencyBoundary 枚举</summary>
public enum IdempotencyBoundary
{
    /// <summary>表示 Http 枚举值</summary>
    Http = 1,
    /// <summary>表示 Grpc 枚举值</summary>
    Grpc = 2,
    /// <summary>表示 Cap 枚举值</summary>
    Cap = 3,
    /// <summary>表示 BackgroundJob 枚举值</summary>
    BackgroundJob = 4
}
