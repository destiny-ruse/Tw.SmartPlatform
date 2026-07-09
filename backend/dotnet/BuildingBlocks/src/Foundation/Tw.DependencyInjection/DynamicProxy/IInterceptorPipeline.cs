using Tw.DynamicProxy.Abstractions;

namespace Tw.DependencyInjection.DynamicProxy;

/// <summary>
/// 编排方法级拦截器调用链
/// </summary>
/// <remarks>
/// 空拦截器链会直接推进原始调用上下文一次。拦截器可以不调用 <see cref="IInvocationContext.ProceedAsync"/> 以短路目标方法。
/// 同一拦截器调用帧只能调用一次 <see cref="IInvocationContext.ProceedAsync"/> 或 <see cref="IInvocationContext.Proceed"/>；
/// 同一调用链中真实目标方法一旦被推进，再次推进真实目标方法会失败。
/// </remarks>
public interface IInterceptorPipeline
{
    /// <summary>
    /// 按给定顺序执行拦截器链，并在链尾推进原始调用上下文
    /// </summary>
    /// <param name="context">原始方法级调用上下文</param>
    /// <param name="interceptors">按执行顺序排列的拦截器实例</param>
    /// <returns>表示拦截链执行完成的 <see cref="ValueTask"/></returns>
    /// <exception cref="ArgumentNullException">context 或 interceptors 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">同一拦截器调用帧重复调用 Proceed，或同一调用链重复推进真实目标方法时抛出</exception>
    ValueTask InvokeAsync(IInvocationContext context, IReadOnlyList<IInterceptor> interceptors);
}
