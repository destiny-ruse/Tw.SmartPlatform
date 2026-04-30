namespace Tw.Core.Configuration;

/// <summary>
/// 标识应绑定到选项类型的配置节
/// </summary>
/// <param name="name">调用方在选项绑定期间使用的非空配置节名称</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class ConfigurationSectionAttribute(string name) : Attribute
{
    /// <summary>
    /// 与被标注选项类型关联的配置节名称
    /// </summary>
    public string Name { get; } = Tw.Core.Check.NotNullOrWhiteSpace(name);

    /// <summary>
    /// Microsoft Options 命名实例名称；为 <see langword="null"/> 时表示默认实例
    /// </summary>
    public string? OptionsName { get; init; }

    /// <summary>
    /// 是否在主机构建完成后立即验证该配置实例；默认 <see langword="true"/> 遵循 fail-fast 原则，
    /// 显式设为 <see langword="false"/> 时跳过启动验证
    /// </summary>
    public bool ValidateOnStart { get; init; } = true;

    /// <summary>
    /// 是否允许直接解析选项类型本体，值来自 <c>IOptionsMonitor&lt;TOptions&gt;.CurrentValue</c>
    /// </summary>
    public bool DirectInject { get; init; }
}
