namespace Tw.Observability;

/// <summary>
/// 封装HealthStatus模型相关的数据和行为
/// </summary>
public sealed record HealthStatusModel(string Status, IReadOnlyDictionary<string, string> Details);
