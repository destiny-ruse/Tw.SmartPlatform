namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for date and time values.</summary>
public static class DateTimeExtensions
{
    /// <summary>Converts a date and time to a Unix timestamp in seconds.</summary>
    /// <param name="dateTime">The date and time to convert.</param>
    /// <returns>The Unix timestamp in seconds.</returns>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();
    }

    /// <summary>Converts a date and time to a Unix timestamp in milliseconds.</summary>
    /// <param name="dateTime">The date and time to convert.</param>
    /// <returns>The Unix timestamp in milliseconds.</returns>
    public static long ToUnixTimestampMilliseconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeMilliseconds();
    }

    /// <summary>Converts a Unix timestamp in seconds to a UTC date and time.</summary>
    /// <param name="timestamp">The Unix timestamp in seconds.</param>
    /// <returns>The UTC date and time.</returns>
    public static DateTime FromUnixTimestamp(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
    }

    /// <summary>Converts a Unix timestamp in milliseconds to a UTC date and time.</summary>
    /// <param name="timestampMilliseconds">The Unix timestamp in milliseconds.</param>
    /// <returns>The UTC date and time.</returns>
    public static DateTime FromUnixTimestampMilliseconds(long timestampMilliseconds)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(timestampMilliseconds).UtcDateTime;
    }

    /// <summary>Returns the first tick of the date's day.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns>The start of the day.</returns>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Kind);
    }

    /// <summary>Returns the last tick of the date's day.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns>The end of the day.</returns>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.StartOfDay().AddDays(1).AddTicks(-1);
    }

    /// <summary>Returns the start of the week, using Monday as the first day.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns>The start of the week.</returns>
    public static DateTime StartOfWeek(this DateTime dateTime)
    {
        var difference = ((int)dateTime.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return dateTime.StartOfDay().AddDays(-difference);
    }

    /// <summary>Returns the last tick of the week, using Monday as the first day.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns>The end of the week.</returns>
    public static DateTime EndOfWeek(this DateTime dateTime)
    {
        return dateTime.StartOfWeek().AddDays(7).AddTicks(-1);
    }

    /// <summary>Returns the first tick of the date's month.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns>The start of the month.</returns>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind);
    }

    /// <summary>Returns the last tick of the date's month.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns>The end of the month.</returns>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return dateTime.StartOfMonth().AddMonths(1).AddTicks(-1);
    }

    /// <summary>Returns the first tick of the date's year.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns>The start of the year.</returns>
    public static DateTime StartOfYear(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind);
    }

    /// <summary>Returns the last tick of the date's year.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns>The end of the year.</returns>
    public static DateTime EndOfYear(this DateTime dateTime)
    {
        return dateTime.StartOfYear().AddYears(1).AddTicks(-1);
    }

    /// <summary>Returns whether the date falls on Saturday or Sunday.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns><see langword="true"/> for weekend dates; otherwise, <see langword="false"/>.</returns>
    public static bool IsWeekend(this DateTime dateTime)
    {
        return dateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    /// <summary>Returns whether the date falls on a weekday.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns><see langword="true"/> for weekday dates; otherwise, <see langword="false"/>.</returns>
    public static bool IsWeekday(this DateTime dateTime)
    {
        return !dateTime.IsWeekend();
    }

    /// <summary>Calculates age in whole years compared with today.</summary>
    /// <param name="dateOfBirth">The birth date.</param>
    /// <returns>The age in whole years.</returns>
    public static int CalculateAge(this DateTime dateOfBirth)
    {
        return dateOfBirth.CalculateAge(DateTime.Today);
    }

    /// <summary>Calculates age in whole years compared with a reference date.</summary>
    /// <param name="dateOfBirth">The birth date.</param>
    /// <param name="today">The reference date.</param>
    /// <returns>The age in whole years.</returns>
    public static int CalculateAge(this DateTime dateOfBirth, DateTime today)
    {
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.Date.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    /// <summary>Returns whether the value is today in local time.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns><see langword="true"/> when the date is today; otherwise, <see langword="false"/>.</returns>
    public static bool IsToday(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.Today;
    }

    /// <summary>Returns whether the value is before the current instant.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns><see langword="true"/> when the value is in the past; otherwise, <see langword="false"/>.</returns>
    public static bool IsPast(this DateTime dateTime)
    {
        return dateTime.ToUniversalTime() < DateTime.UtcNow;
    }

    /// <summary>Returns whether the value is after the current instant.</summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns><see langword="true"/> when the value is in the future; otherwise, <see langword="false"/>.</returns>
    public static bool IsFuture(this DateTime dateTime)
    {
        return dateTime.ToUniversalTime() > DateTime.UtcNow;
    }

    /// <summary>Formats the date and time using a friendly general pattern.</summary>
    /// <param name="dateTime">The date and time to format.</param>
    /// <returns>A friendly date and time string.</returns>
    public static string ToFriendlyString(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
