namespace Tw.DynamicProxy.Abstractions;

/// <summary>
/// 异步拦截器基类，按 BeforeAsync / ProceedAsync / OnExceptionAsync / AfterAsync 编排
/// </summary>
public abstract class InterceptorBase : IInterceptor
{
    /// <inheritdoc />
    public async ValueTask InterceptAsync(IInvocationContext context)
    {
        await BeforeAsync(context);
        try
        {
            await context.ProceedAsync();
        }
        catch (Exception ex)
        {
            await OnExceptionAsync(context, ex);
            throw;
        }
        finally
        {
            await AfterAsync(context);
        }
    }

    /// <summary>目标方法执行前调用</summary>
    /// <param name="context">调用上下文</param>
    /// <returns>表示前置逻辑完成的 <see cref="ValueTask"/></returns>
    protected virtual ValueTask BeforeAsync(IInvocationContext context) => ValueTask.CompletedTask;

    /// <summary>目标方法执行后调用，无论是否抛异常都在 finally 中执行</summary>
    /// <param name="context">调用上下文</param>
    /// <returns>表示后置逻辑完成的 <see cref="ValueTask"/></returns>
    protected virtual ValueTask AfterAsync(IInvocationContext context) => ValueTask.CompletedTask;

    /// <summary>目标方法抛异常时调用，默认不吞异常</summary>
    /// <param name="context">调用上下文</param>
    /// <param name="exception">目标方法抛出的异常</param>
    /// <returns>表示异常处理完成的 <see cref="ValueTask"/></returns>
    protected virtual ValueTask OnExceptionAsync(IInvocationContext context, Exception exception) =>
        ValueTask.CompletedTask;
}
