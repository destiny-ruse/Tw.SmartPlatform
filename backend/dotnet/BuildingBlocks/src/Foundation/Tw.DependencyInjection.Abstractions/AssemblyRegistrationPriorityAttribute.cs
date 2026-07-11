namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 声明程序集级显式注册优先级
/// </summary>
/// <remarks>配置 <c>Tw:DependencyInjection:AssemblyPriorities</c> 优先于本特性。</remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class AssemblyRegistrationPriorityAttribute : Attribute
{
    /// <summary>
    /// 声明程序集级优先级
    /// </summary>
    /// <param name="priority">优先级数值，越大优先级越高</param>
    public AssemblyRegistrationPriorityAttribute(int priority)
    {
        Priority = priority;
    }

    /// <summary>
    /// 程序集级优先级
    /// </summary>
    public int Priority { get; }
}
