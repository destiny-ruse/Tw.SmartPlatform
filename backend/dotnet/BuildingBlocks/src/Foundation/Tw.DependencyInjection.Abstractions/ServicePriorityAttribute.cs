namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 声明类型级显式注册优先级
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ServicePriorityAttribute : Attribute
{
    /// <summary>
    /// 声明类型级优先级
    /// </summary>
    /// <param name="priority">优先级数值，越大优先级越高</param>
    public ServicePriorityAttribute(int priority)
    {
        Priority = priority;
    }

    /// <summary>
    /// 类型级优先级
    /// </summary>
    public int Priority { get; }
}
