namespace Tw.Core.Timing;

public interface IClock
{
    DateTime Now { get; }

    DateTimeKind Kind { get; }

    bool SupportsMultipleTimezone { get; }

    DateTime Normalize(DateTime dateTime);

    DateTime ConvertToUserTime(DateTime utcDateTime);

    DateTimeOffset ConvertToUserTime(DateTimeOffset dateTimeOffset);

    DateTime ConvertToUtc(DateTime dateTime);
}
