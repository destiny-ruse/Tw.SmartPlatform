namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 声明类型参与自动注册，并可指定生命周期与类型级优先级
/// </summary>
/// <remarks>
/// 本特性不承载服务替换语义；同一契约多个候选由优先级单实现仲裁决定唯一胜者。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ServiceRegistrationAttribute : Attribute
{
    /// <summary>使用默认规则注册，生命周期由标记接口决定</summary>
    public ServiceRegistrationAttribute()
    {
    }

    /// <summary>使用显式生命周期注册</summary>
    /// <param name="lifetime">服务生命周期</param>
    public ServiceRegistrationAttribute(DependencyLifetime lifetime)
    {
        Lifetime = lifetime;
    }

    /// <summary>显式生命周期；为 <see langword="null"/> 时回退到标记接口</summary>
    public DependencyLifetime? Lifetime { get; }

    /// <summary>类型级显式优先级，参与单实现仲裁</summary>
    public int Priority { get; set; }
}
