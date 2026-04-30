namespace Tw.DependencyInjection.Registration.Attributes;

/// <summary>
/// 标记实现类型退出自动注册扫描。
/// 被标注的类型将被扫描器完全忽略。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class DisableAutoRegistrationAttribute : Attribute;
