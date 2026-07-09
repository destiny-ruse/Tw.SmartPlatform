namespace Tw.Features;

/// <summary>
/// Feature 定义
/// </summary>
/// <param name="Name">Feature 名称</param>
/// <param name="DefaultEnabled">未配置作用域值时的默认启用状态</param>
public sealed record FeatureDefinition(string Name, bool DefaultEnabled);
