namespace Tw.Features;

/// <summary>
/// 指定作用域下的 Feature 值
/// </summary>
/// <param name="Name">Feature 名称</param>
/// <param name="Scope">Feature 值作用域</param>
/// <param name="ScopeKey">作用域键</param>
/// <param name="Enabled">是否启用</param>
/// <param name="Version">值版本</param>
public sealed record FeatureValue(string Name, FeatureScope Scope, string ScopeKey, bool Enabled, long Version);
