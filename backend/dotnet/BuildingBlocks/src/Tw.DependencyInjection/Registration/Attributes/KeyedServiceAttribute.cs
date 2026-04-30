namespace Tw.DependencyInjection.Registration.Attributes;

/// <summary>
/// 为实现类型指定命名键，以参与基于键的服务解析。
/// 不支持与开放泛型类型定义同时使用。
/// </summary>
/// <param name="key">用于区分同一接口多个实现的非空键值。</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class KeyedServiceAttribute(object key) : Attribute
{
    /// <summary>
    /// 服务注册所使用的键值。
    /// </summary>
    public object Key { get; } = key;
}
