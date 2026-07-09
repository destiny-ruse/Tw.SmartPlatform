namespace Tw.Settings;

/// <summary>
/// Setting 缓存键
/// </summary>
/// <param name="Name">Setting 名称</param>
/// <param name="Scope">Setting 作用域</param>
/// <param name="ScopeKey">作用域键</param>
public sealed record SettingCacheKey(string Name, SettingScope Scope, string ScopeKey);
