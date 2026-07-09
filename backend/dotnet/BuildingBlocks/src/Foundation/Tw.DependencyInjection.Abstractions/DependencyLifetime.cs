namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 自动注册服务的生命周期
/// </summary>
/// <remarks>
/// "未指定生命周期"一律用可空形式 <c>DependencyLifetime?</c> 的 <see langword="null"/> 表达，
/// 不得把 <c>default(DependencyLifetime)</c> 当作未设置哨兵；引擎层将 <see langword="null"/> 映射为"由标记接口决定"。
/// 成员数字值是稳定契约：<c>Transient=0</c>、<c>Scoped=1</c>、<c>Singleton=2</c>，不得调整顺序。
/// </remarks>
public enum DependencyLifetime
{
    /// <summary>每次解析创建新实例</summary>
    Transient,

    /// <summary>每个作用域一个实例</summary>
    Scoped,

    /// <summary>容器全局单例</summary>
    Singleton,
}
