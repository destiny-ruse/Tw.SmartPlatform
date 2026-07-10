namespace Tw.Idempotency;

/// <summary>
/// 定义幂等请求占用状态
/// </summary>
public enum IdempotencyReservationStatus
{
    /// <summary>
    /// 表示 Started 枚举值
    /// </summary>
    Started = 1,
    /// <summary>
    /// 表示 Duplicate 枚举值
    /// </summary>
    Duplicate = 2,
    /// <summary>
    /// 表示 Conflict 枚举值
    /// </summary>
    Conflict = 3
}

/// <summary>
/// 封装幂等请求占用结果的数据
/// </summary>
public sealed record IdempotencyReservation(IdempotencyReservationStatus Status, IdempotencyResult? ExistingResult);
