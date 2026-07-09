namespace Tw.Observability;

/// <summary>表示 HealthStatusModel 声明</summary>
public sealed record HealthStatusModel(string Status, IReadOnlyDictionary<string, string> Details);
