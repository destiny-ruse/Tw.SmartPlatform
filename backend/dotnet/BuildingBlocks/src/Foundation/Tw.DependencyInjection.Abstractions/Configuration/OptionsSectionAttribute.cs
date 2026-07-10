namespace Tw.DependencyInjection.Abstractions.Configuration;

/// <summary>
/// 显式声明选项绑定的配置节路径
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OptionsSectionAttribute : Attribute
{
    /// <summary>
    /// 声明配置节路径
    /// </summary>
    /// <param name="path">配置节路径，例如 <c>Tw:Cache</c></param>
    public OptionsSectionAttribute(string path)
    {
        Path = path;
    }

    /// <summary>
    /// 配置节路径
    /// </summary>
    public string Path { get; }
}
