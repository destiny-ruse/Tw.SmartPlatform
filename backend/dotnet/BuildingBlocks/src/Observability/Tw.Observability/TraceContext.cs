namespace Tw.Observability;

public sealed record TraceContext(string TraceId, string? SpanId, string OperationName);
