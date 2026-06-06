namespace Tw.DynamicProxy.Abstractions;

/// <summary>
/// 统一方法级拦截器契约
/// </summary>
public interface IInterceptor
{
    /// <summary>拦截一次方法级调用</summary>
    /// <param name="context">方法级调用上下文</param>
    /// <returns>表示拦截完成的 <see cref="ValueTask"/></returns>
    ValueTask InterceptAsync(IInvocationContext context);
}
