namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 声明类型以指定 key 注册为 keyed service
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ExposeKeyedServiceAttribute : Attribute
{
    /// <summary>声明 keyed 注册</summary>
    /// <param name="serviceType">服务契约类型</param>
    /// <param name="key">稳定 key，不可为空</param>
    public ExposeKeyedServiceAttribute(Type serviceType, object key)
    {
        ServiceType = serviceType;
        Key = key;
    }

    /// <summary>服务契约类型</summary>
    public Type ServiceType { get; }

    /// <summary>注册 key</summary>
    public object Key { get; }
}
