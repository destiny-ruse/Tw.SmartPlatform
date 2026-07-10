namespace Tw.Castle.Core.Abstractions;

/// <summary>
/// 声明类、接口或方法启用指定拦截器
/// </summary>
/// <remarks>方法级声明优先于类型级。</remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class InterceptAttribute : Attribute
{
    /// <summary>
    /// 声明拦截器类型
    /// </summary>
    /// <param name="interceptorType">实现 <see cref="IInterceptor"/> 的拦截器类型</param>
    public InterceptAttribute(Type interceptorType)
    {
        InterceptorType = interceptorType;
    }

    /// <summary>
    /// 拦截器类型
    /// </summary>
    public Type InterceptorType { get; }
}
