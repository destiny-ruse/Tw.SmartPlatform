namespace Tw.Configuration.Abstractions;

/// <summary>
/// 标记选项类型跳过自动绑定
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DisableOptionsBindingAttribute : Attribute;
