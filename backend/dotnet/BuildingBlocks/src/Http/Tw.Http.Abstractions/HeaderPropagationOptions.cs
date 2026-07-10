namespace Tw.Http.Abstractions;

/// <summary>
/// 配置HeaderPropagation的运行行为
/// </summary>
public sealed record HeaderPropagationOptions(IReadOnlySet<string> AllowedHeaders);
