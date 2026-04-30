namespace Tw.DependencyInjection.Registration.Attributes;

/// <summary>
/// 标记实现类型以集合追加模式注册，允许同一服务接口存在多个实现共存。
/// 注册时按 <see cref="Order"/> 升序排列。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CollectionServiceAttribute : Attribute
{
    /// <summary>
    /// 在同一服务集合中的注册顺序，值越小越靠前。
    /// </summary>
    public int Order { get; init; }
}
