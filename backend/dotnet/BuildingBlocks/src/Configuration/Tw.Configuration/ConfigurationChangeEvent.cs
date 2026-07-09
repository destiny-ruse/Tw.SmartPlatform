namespace Tw.Configuration;

public sealed record ConfigurationChangeEvent(string Key, string Source, DateTimeOffset ChangedAt);
