using Tw.DependencyInjection.Invocation;

namespace Tw.DependencyInjection.Interception;

/// <summary>
/// 所有运行时拦截器的基类
/// </summary>
public abstract class InterceptorBase
{
    /// <summary>
    /// 拦截单次方法调用
    /// </summary>
    /// <param name="context">当前方法调用上下文</param>
    public virtual ValueTask InterceptAsync(IUnaryInvocationContext context)
        => context.ProceedAsync();

    /// <summary>
    /// 拦截异步流式结果
    /// </summary>
    /// <typeparam name="T">流式元素类型</typeparam>
    /// <param name="context">当前调用上下文</param>
    /// <param name="source">下游异步流</param>
    public virtual IAsyncEnumerable<T> InterceptAsyncEnumerable<T>(
        IInvocationContext context,
        IAsyncEnumerable<T> source)
        => source;
}
