namespace Tw.Features;

/// <summary>
/// Feature 缓存键
/// </summary>
/// <param name="Name">Feature 名称</param>
/// <param name="Scope">Feature 值作用域</param>
/// <param name="ScopeKey">作用域键</param>
public sealed record FeatureCacheKey(string Name, FeatureScope Scope, string ScopeKey);
