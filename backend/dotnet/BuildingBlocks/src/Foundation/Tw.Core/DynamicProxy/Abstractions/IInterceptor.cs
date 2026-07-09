namespace Tw.DynamicProxy.Abstractions;

/// <summary>
/// 统一方法级拦截器契约
/// </summary>
/// <remarks>
/// 本接口与 <c>Castle.DynamicProxy.IInterceptor</c> 简单名相同、命名空间不同。
/// 引擎内部适配器同时引用两者时，使用命名空间限定或 <c>using alias</c>（如 <c>using CastleInterceptor = Castle.DynamicProxy.IInterceptor;</c>）区分。
/// </remarks>
public interface IInterceptor
{
    /// <summary>拦截一次方法级调用</summary>
    /// <param name="context">方法级调用上下文</param>
    /// <returns>表示拦截完成的 <see cref="ValueTask"/></returns>
    ValueTask InterceptAsync(IInvocationContext context);
}
