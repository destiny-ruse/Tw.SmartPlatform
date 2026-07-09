namespace Tw.Resilience;

/// <summary>定义 OperationKind 枚举</summary>
public enum OperationKind
{
    /// <summary>表示 Read 枚举值</summary>
    Read = 1,
    /// <summary>表示 IdempotentWrite 枚举值</summary>
    IdempotentWrite = 2,
    /// <summary>表示 NonIdempotentWrite 枚举值</summary>
    NonIdempotentWrite = 3
}
