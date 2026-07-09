namespace Tw.DependencyInjection.Abstractions.Configuration;

/// <summary>
/// 标记选项类型跳过自动绑定
/// </summary>
/// <remarks>即使类型实现 <see cref="IConfigurableOptions"/>，标注此特性后绑定引擎也会跳过自动绑定与 PostConfigure 执行。</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DisableOptionsBindingAttribute : Attribute;
