namespace Tw.Observability;

/// <summary>表示 TraceContext 声明</summary>
public sealed record TraceContext(string TraceId, string? SpanId, string OperationName);
