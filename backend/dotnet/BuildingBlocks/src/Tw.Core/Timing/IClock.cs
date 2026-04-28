namespace Tw.Core.Timing;

/// <summary>
/// 提供当前时间与时区规范化的应用时钟契约
/// </summary>
public interface IClock
{
    /// <summary>
    /// 按时钟配置的时间种类表示的当前日期时间
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// 此时钟生成的 <see cref="DateTimeKind"/>
    /// </summary>
    DateTimeKind Kind { get; }

    /// <summary>
    /// 时钟是否可在 UTC 与用户本地时区之间转换
    /// </summary>
    /// <remarks>
    /// 当为 <see langword="false"/> 时，调用方应将用户时间转换视为不可用，
    /// 或视为等同于时钟配置的时间种类
    /// </remarks>
    bool SupportsMultipleTimezone { get; }

    /// <summary>
    /// 将给定日期时间规范化为时钟配置的 <see cref="Kind"/>
    /// </summary>
    /// <param name="dateTime">要规范化的日期时间值</param>
    /// <returns>规范化后的日期时间值</returns>
    DateTime Normalize(DateTime dateTime);

    /// <summary>
    /// 当支持多时区时，将 UTC 日期时间转换为当前用户时区
    /// </summary>
    /// <param name="utcDateTime">要转换的 UTC 日期时间</param>
    /// <returns>转换后的用户本地日期时间；用户时间不可用时返回时钟特定的等价值</returns>
    DateTime ConvertToUserTime(DateTime utcDateTime);

    /// <summary>
    /// 当支持多时区时，将日期时间偏移转换为当前用户时区
    /// </summary>
    /// <param name="dateTimeOffset">要转换的日期时间偏移</param>
    /// <returns>转换后的用户本地日期时间偏移；用户时间不可用时返回时钟特定的等价值</returns>
    DateTimeOffset ConvertToUserTime(DateTimeOffset dateTimeOffset);

    /// <summary>
    /// 按时钟的时区规则将给定日期时间转换为 UTC
    /// </summary>
    /// <param name="dateTime">要转换为 UTC 的日期时间</param>
    /// <returns>UTC 日期时间值</returns>
    DateTime ConvertToUtc(DateTime dateTime);
}
