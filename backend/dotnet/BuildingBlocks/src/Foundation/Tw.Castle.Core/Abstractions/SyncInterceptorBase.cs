namespace Tw.Castle.Core.Abstractions;

/// <summary>
/// 同步拦截器基类，按 Before / Proceed / OnException / After 编排
/// </summary>
/// <remarks>
/// 仅用于同步目标方法；误用于异步目标时由 <see cref="IInvocationContext.Proceed"/> 在运行期抛出明确异常。
/// <see cref="Before"/> 在保护区之外执行：若它抛出异常，目标方法不会被调用，<see cref="OnException"/> 与 <see cref="After"/> 也不会执行，异常直接向上传播。
/// </remarks>
public abstract class SyncInterceptorBase : IInterceptor
{
    /// <inheritdoc />
    public ValueTask InterceptAsync(IInvocationContext context)
    {
        Before(context);
        try
        {
            context.Proceed();
        }
        catch (Exception ex)
        {
            OnException(context, ex);
            throw;
        }
        finally
        {
            After(context);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 目标方法执行前调用
    /// </summary>
    /// <param name="context">调用上下文</param>
    protected virtual void Before(IInvocationContext context)
    {
    }

    /// <summary>
    /// 目标方法执行后调用，无论是否抛异常都在 finally 中执行
    /// </summary>
    /// <param name="context">调用上下文</param>
    protected virtual void After(IInvocationContext context)
    {
    }

    /// <summary>
    /// 目标方法抛异常时调用，默认不吞异常
    /// </summary>
    /// <param name="context">调用上下文</param>
    /// <param name="exception">目标方法抛出的异常</param>
    protected virtual void OnException(IInvocationContext context, Exception exception)
    {
    }
}
