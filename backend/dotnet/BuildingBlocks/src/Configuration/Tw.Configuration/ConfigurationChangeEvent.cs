namespace Tw.Configuration;

/// <summary>
/// 封装ConfigurationChange事件相关的数据和行为
/// </summary>
public sealed record ConfigurationChangeEvent(string Key, string Source, DateTimeOffset ChangedAt);
