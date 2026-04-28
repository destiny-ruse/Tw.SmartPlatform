namespace Tw.Core.Timing;

/// <summary>
/// Provides the application clock contract for current time and time-zone normalization.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current date and time in the clock's configured kind.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets the <see cref="DateTimeKind"/> produced by this clock.
    /// </summary>
    DateTimeKind Kind { get; }

    /// <summary>
    /// Gets whether the clock can convert between UTC and user-local time zones.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, callers should treat user-time conversion as unavailable or equivalent
    /// to the clock's configured time kind.
    /// </remarks>
    bool SupportsMultipleTimezone { get; }

    /// <summary>
    /// Normalizes the supplied date and time to the clock's configured <see cref="Kind"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value to normalize.</param>
    /// <returns>The normalized date and time value.</returns>
    DateTime Normalize(DateTime dateTime);

    /// <summary>
    /// Converts a UTC date and time to the current user's time zone when multiple time zones are supported.
    /// </summary>
    /// <param name="utcDateTime">The UTC date and time to convert.</param>
    /// <returns>The converted user-local date and time, or the clock-specific equivalent when user time is unavailable.</returns>
    DateTime ConvertToUserTime(DateTime utcDateTime);

    /// <summary>
    /// Converts a date and time offset to the current user's time zone when multiple time zones are supported.
    /// </summary>
    /// <param name="dateTimeOffset">The date and time offset to convert.</param>
    /// <returns>The converted user-local date and time offset, or the clock-specific equivalent when user time is unavailable.</returns>
    DateTimeOffset ConvertToUserTime(DateTimeOffset dateTimeOffset);

    /// <summary>
    /// Converts the supplied date and time to UTC according to the clock's time-zone rules.
    /// </summary>
    /// <param name="dateTime">The date and time to convert to UTC.</param>
    /// <returns>The UTC date and time value.</returns>
    DateTime ConvertToUtc(DateTime dateTime);
}
