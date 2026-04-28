namespace Tw.Core.Extensions;

/// <summary>提供日期时间值的扩展方法</summary>
public static class DateTimeExtensions
{
    /// <summary>将日期时间转换为秒级 Unix 时间戳</summary>
    /// <param name="dateTime">要转换的日期时间</param>
    /// <returns>秒级 Unix 时间戳</returns>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();
    }

    /// <summary>将日期时间转换为毫秒级 Unix 时间戳</summary>
    /// <param name="dateTime">要转换的日期时间</param>
    /// <returns>毫秒级 Unix 时间戳</returns>
    public static long ToUnixTimestampMilliseconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeMilliseconds();
    }

    /// <summary>将秒级 Unix 时间戳转换为 UTC 日期时间</summary>
    /// <param name="timestamp">秒级 Unix 时间戳</param>
    /// <returns>UTC 日期时间</returns>
    public static DateTime FromUnixTimestamp(this long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
    }

    /// <summary>将毫秒级 Unix 时间戳转换为 UTC 日期时间</summary>
    /// <param name="timestampMilliseconds">毫秒级 Unix 时间戳</param>
    /// <returns>UTC 日期时间</returns>
    public static DateTime FromUnixTimestampMilliseconds(this long timestampMilliseconds)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(timestampMilliseconds).UtcDateTime;
    }

    /// <summary>返回日期当天的第一个计时周期</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>当天开始时间</returns>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Kind);
    }

    /// <summary>返回日期当天的最后一个计时周期</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>当天结束时间</returns>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        if (dateTime.Date == DateTime.MaxValue.Date)
        {
            return DateTime.MaxValue;
        }

        return dateTime.StartOfDay().AddDays(1).AddTicks(-1);
    }

    /// <summary>以星期一为每周第一天返回周开始时间</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>周开始时间</returns>
    public static DateTime StartOfWeek(this DateTime dateTime)
    {
        var difference = ((int)dateTime.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        if (dateTime.StartOfDay().Ticks < TimeSpan.TicksPerDay * difference)
        {
            return DateTime.MinValue;
        }

        return dateTime.StartOfDay().AddDays(-difference);
    }

    /// <summary>以星期一为每周第一天返回周最后一个计时周期</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>周结束时间</returns>
    public static DateTime EndOfWeek(this DateTime dateTime)
    {
        var startOfWeek = dateTime.StartOfWeek();
        return DateTime.MaxValue.Ticks - startOfWeek.Ticks < TimeSpan.TicksPerDay * 7
            ? DateTime.MaxValue
            : startOfWeek.AddDays(7).AddTicks(-1);
    }

    /// <summary>返回日期所在月份的第一个计时周期</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>月开始时间</returns>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind);
    }

    /// <summary>返回日期所在月份的最后一个计时周期</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>月结束时间</returns>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        if (dateTime.Year == DateTime.MaxValue.Year && dateTime.Month == DateTime.MaxValue.Month)
        {
            return DateTime.MaxValue;
        }

        return dateTime.StartOfMonth().AddMonths(1).AddTicks(-1);
    }

    /// <summary>返回日期所在年份的第一个计时周期</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>年开始时间</returns>
    public static DateTime StartOfYear(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind);
    }

    /// <summary>返回日期所在年份的最后一个计时周期</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>年结束时间</returns>
    public static DateTime EndOfYear(this DateTime dateTime)
    {
        if (dateTime.Year == DateTime.MaxValue.Year)
        {
            return DateTime.MaxValue;
        }

        return dateTime.StartOfYear().AddYears(1).AddTicks(-1);
    }

    /// <summary>返回日期是否落在星期六或星期日</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>周末日期返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsWeekend(this DateTime dateTime)
    {
        return dateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    /// <summary>返回日期是否落在工作日</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>工作日日期返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsWeekday(this DateTime dateTime)
    {
        return !dateTime.IsWeekend();
    }

    /// <summary>相对于今天计算完整年数年龄</summary>
    /// <param name="birthDate">出生日期</param>
    /// <returns>完整年数年龄</returns>
    public static int CalculateAge(this DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.Date.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    /// <summary>返回该值在本地时间下是否为今天</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>日期为今天时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsToday(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.Today;
    }

    /// <summary>返回该值是否早于当前时刻</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>值位于过去时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsPast(this DateTime dateTime)
    {
        return dateTime.ToUniversalTime() < DateTime.UtcNow;
    }

    /// <summary>返回该值是否晚于当前时刻</summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>值位于未来时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsFuture(this DateTime dateTime)
    {
        return dateTime.ToUniversalTime() > DateTime.UtcNow;
    }

    /// <summary>使用友好的通用模式格式化日期时间</summary>
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>友好的日期时间字符串</returns>
    public static string ToFriendlyString(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
