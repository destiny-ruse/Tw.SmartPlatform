namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 标记类型跳过自动注册
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DisableServiceRegistrationAttribute : Attribute;
