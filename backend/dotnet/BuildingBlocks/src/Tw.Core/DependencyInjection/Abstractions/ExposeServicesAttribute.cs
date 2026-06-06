namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 显式声明类型对外暴露的服务契约
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ExposeServicesAttribute : Attribute
{
    /// <summary>声明要暴露的契约类型</summary>
    /// <param name="serviceTypes">对外暴露的契约类型</param>
    public ExposeServicesAttribute(params Type[] serviceTypes)
    {
        ServiceTypes = serviceTypes;
    }

    /// <summary>对外暴露的契约类型</summary>
    public IReadOnlyList<Type> ServiceTypes { get; }

    /// <summary>是否同时暴露实现类自身类型</summary>
    public bool IncludeSelf { get; set; }
}
