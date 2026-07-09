namespace Tw.Timing;

/// <summary>
/// 提供当前时间的抽象，供业务代码消除对系统时间的直接依赖
/// </summary>
public interface IClock
{
    /// <summary>
    /// 当前时间
    /// </summary>
    DateTimeOffset Now { get; }
}
