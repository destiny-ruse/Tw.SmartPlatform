namespace Tw.DynamicProxy.Abstractions;

/// <summary>
/// 关闭类或方法的拦截
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DisableInterceptionAttribute : Attribute;
