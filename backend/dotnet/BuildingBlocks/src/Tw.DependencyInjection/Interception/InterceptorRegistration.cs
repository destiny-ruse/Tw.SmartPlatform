using Tw.Core;

namespace Tw.DependencyInjection.Interception;

/// <summary>
/// 拦截器链中的单条拦截器注册记录
/// </summary>
public sealed record InterceptorRegistration
{
    /// <summary>
    /// 初始化拦截器注册记录
    /// </summary>
    /// <param name="interceptorType">运行时拦截器类型</param>
    /// <param name="scope">拦截器作用域</param>
    /// <param name="order">同层排序值</param>
    public InterceptorRegistration(Type interceptorType, InterceptorScope scope, int order = 0)
    {
        InterceptorType = Check.AssignableTo<InterceptorBase>(interceptorType);
        Scope = scope;
        Order = order;
    }

    /// <summary>
    /// 运行时拦截器类型
    /// </summary>
    public Type InterceptorType { get; init; }

    /// <summary>
    /// 拦截器作用域
    /// </summary>
    public InterceptorScope Scope { get; init; }

    /// <summary>
    /// 同层排序值
    /// </summary>
    public int Order { get; init; }
}
