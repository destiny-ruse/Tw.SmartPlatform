namespace Tw.Castle.Core.Abstractions;

/// <summary>
/// 关闭类或方法的拦截
/// </summary>
/// <remarks>
/// 标注在方法上仅关闭该方法的拦截；标注在类上关闭该类全部方法的拦截。
/// 与 <see cref="InterceptAttribute"/> 同时出现时的跨层级优先级由 AOP 引擎规则定义。
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DisableInterceptionAttribute : Attribute;
