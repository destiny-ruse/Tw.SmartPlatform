namespace Tw.DependencyInjection.Registration.Attributes;

/// <summary>
/// 显式声明实现类型对外暴露的服务接口列表。
/// 不标注时，扫描器按默认规则推导暴露接口。
/// </summary>
/// <param name="serviceTypes">要暴露的服务类型列表，允许包含开放泛型定义。</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class ExposeServicesAttribute(params Type[] serviceTypes) : Attribute
{
    /// <summary>
    /// 要对外暴露的服务类型只读列表。
    /// </summary>
    public IReadOnlyList<Type> ServiceTypes { get; } = serviceTypes;

    /// <summary>
    /// 是否在暴露列表中额外追加实现类型本身。
    /// </summary>
    public bool IncludeSelf { get; init; }
}
