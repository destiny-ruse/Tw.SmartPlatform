namespace Tw.Http.Abstractions;

/// <summary>表示 HeaderPropagationOptions 声明</summary>
public sealed record HeaderPropagationOptions(IReadOnlySet<string> AllowedHeaders);
