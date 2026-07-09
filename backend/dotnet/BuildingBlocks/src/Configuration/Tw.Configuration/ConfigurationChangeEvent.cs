namespace Tw.Configuration;

/// <summary>表示 ConfigurationChangeEvent 声明</summary>
public sealed record ConfigurationChangeEvent(string Key, string Source, DateTimeOffset ChangedAt);
