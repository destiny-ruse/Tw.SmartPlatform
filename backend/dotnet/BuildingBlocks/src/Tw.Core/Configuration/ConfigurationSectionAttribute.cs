namespace Tw.Core.Configuration;

/// <summary>
/// 标识应绑定到选项类型的配置节
/// </summary>
/// <param name="name">调用方在选项绑定期间使用的非空配置节名称</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ConfigurationSectionAttribute(string name) : Attribute
{
    /// <summary>
    /// 与被标注选项类型关联的配置节名称
    /// </summary>
    public string Name { get; } = Tw.Core.Check.NotNullOrWhiteSpace(name);
}
