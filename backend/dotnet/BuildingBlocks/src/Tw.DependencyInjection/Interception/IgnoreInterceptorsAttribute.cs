using Tw.Core;

namespace Tw.DependencyInjection.Interception;

/// <summary>
/// 声明当前类型忽略部分或全部自动拦截器
/// </summary>
/// <param name="interceptorTypes">要忽略的拦截器类型；为空时忽略全部全局和自动匹配拦截器</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
public sealed class IgnoreInterceptorsAttribute(params Type[] interceptorTypes) : Attribute
{
    /// <summary>
    /// 要忽略的拦截器类型只读列表；空列表表示忽略全部全局和自动匹配拦截器
    /// </summary>
    public IReadOnlyList<Type> InterceptorTypes { get; } = Check.NotNull(interceptorTypes)
        .Select(type => Check.AssignableTo<InterceptorBase>(type))
        .ToArray();
}
