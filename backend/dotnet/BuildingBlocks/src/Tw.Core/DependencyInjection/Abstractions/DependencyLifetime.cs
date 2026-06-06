namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 自动注册服务的生命周期
/// </summary>
public enum DependencyLifetime
{
    /// <summary>每次解析创建新实例</summary>
    Transient,

    /// <summary>每个作用域一个实例</summary>
    Scoped,

    /// <summary>容器全局单例</summary>
    Singleton,
}
