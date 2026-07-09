namespace Tw.Observability;

public sealed record HealthStatusModel(string Status, IReadOnlyDictionary<string, string> Details);
