namespace Tw.EventBus.Cap.Consumers;

/// <summary>
/// 定义 CapConsumerStatus 枚举
/// </summary>
public enum CapConsumerStatus
{
    /// <summary>
    /// 表示 Succeeded 枚举值
    /// </summary>
    Succeeded = 1,
    /// <summary>
    /// 表示 Duplicate 枚举值
    /// </summary>
    Duplicate = 2
}

/// <summary>
/// 承载CapConsumer处理后的结果数据
/// </summary>
public sealed record CapConsumerResult(CapConsumerStatus Status);
