namespace Tw.Observability;

/// <summary>
/// 封装Trace上下文相关的数据和行为
/// </summary>
public sealed record TraceContext(string TraceId, string? SpanId, string OperationName);
