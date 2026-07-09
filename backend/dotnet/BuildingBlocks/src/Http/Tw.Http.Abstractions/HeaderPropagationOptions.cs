namespace Tw.Http.Abstractions;

public sealed record HeaderPropagationOptions(IReadOnlySet<string> AllowedHeaders);
