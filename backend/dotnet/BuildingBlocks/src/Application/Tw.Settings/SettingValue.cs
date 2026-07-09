namespace Tw.Settings;

/// <summary>
/// 指定作用域下的 Setting 值
/// </summary>
/// <param name="Name">Setting 名称</param>
/// <param name="Scope">Setting 作用域</param>
/// <param name="ScopeKey">作用域键</param>
/// <param name="Value">Setting 值</param>
/// <param name="Version">值版本号</param>
public sealed record SettingValue(
    string Name,
    SettingScope Scope,
    string ScopeKey,
    string Value,
    long Version);
