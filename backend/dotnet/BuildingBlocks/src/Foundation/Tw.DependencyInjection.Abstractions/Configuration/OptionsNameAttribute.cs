namespace Tw.DependencyInjection.Abstractions.Configuration;

/// <summary>
/// 为选项类型声明命名实例
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OptionsNameAttribute : Attribute
{
    /// <summary>
    /// 声明命名实例名称
    /// </summary>
    /// <param name="name">命名实例名称</param>
    public OptionsNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 命名实例名称
    /// </summary>
    public string Name { get; }
}
