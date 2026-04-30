namespace Tw.DependencyInjection.Registration.Attributes;

/// <summary>
/// 标记实现类型以替换模式注册，将覆盖已注册的同一服务接口实现。
/// 存在多个替换候选时按 <see cref="Order"/> 升序决定最终生效者。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ReplaceServiceAttribute : Attribute
{
    /// <summary>
    /// 替换优先级，值越小优先级越高。
    /// </summary>
    public int Order { get; init; }
}
