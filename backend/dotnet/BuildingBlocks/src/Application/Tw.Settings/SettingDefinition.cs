namespace Tw.Settings;

/// <summary>
/// Setting 定义及默认值
/// </summary>
/// <param name="Name">Setting 名称</param>
/// <param name="DefaultValue">默认值</param>
public sealed record SettingDefinition(string Name, string DefaultValue);
