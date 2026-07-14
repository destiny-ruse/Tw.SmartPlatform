namespace Tw.Resilience;

/// <summary>
/// 标识操作是否具备安全自动重试的幂等前提
/// </summary>
public enum OperationKind
{
    /// <summary>
    /// 不产生业务写入副作用的读取操作
    /// </summary>
    Read = 1,

    /// <summary>
    /// 具备明确幂等键或等价重复提交保护的写操作
    /// </summary>
    IdempotentWrite = 2,

    /// <summary>
    /// 重复执行可能产生额外业务副作用的写操作
    /// </summary>
    NonIdempotentWrite = 3
}
