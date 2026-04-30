using Tw.Core;

namespace Tw.DependencyInjection.Interception;

/// <summary>
/// 显式声明服务层必须应用的拦截器
/// </summary>
/// <param name="interceptorType">运行时拦截器类型</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
public sealed class InterceptAttribute(Type interceptorType) : Attribute
{
    /// <summary>
    /// 运行时拦截器类型
    /// </summary>
    public Type InterceptorType { get; } = Check.AssignableTo<InterceptorBase>(interceptorType);
}
