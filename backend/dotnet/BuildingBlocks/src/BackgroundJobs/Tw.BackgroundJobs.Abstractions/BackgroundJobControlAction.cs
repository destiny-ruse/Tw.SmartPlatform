namespace Tw.BackgroundJobs.Abstractions;

/// <summary>
/// 定义 BackgroundJobControlAction 枚举
/// </summary>
public enum BackgroundJobControlAction
{
    /// <summary>
    /// 表示 Create 枚举值
    /// </summary>
    Create = 1,
    /// <summary>
    /// 表示 Pause 枚举值
    /// </summary>
    Pause = 2,
    /// <summary>
    /// 表示 Resume 枚举值
    /// </summary>
    Resume = 3,
    /// <summary>
    /// 表示 Trigger 枚举值
    /// </summary>
    Trigger = 4,
    /// <summary>
    /// 表示 Stop 枚举值
    /// </summary>
    Stop = 5
}
