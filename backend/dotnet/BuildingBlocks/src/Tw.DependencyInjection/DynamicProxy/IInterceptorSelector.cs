using System.Reflection;

namespace Tw.DependencyInjection.DynamicProxy;

/// <summary>
/// 根据服务、实现与方法契约选择参与调用链的拦截器类型
/// </summary>
/// <remarks>
/// 当服务契约为接口时，实现应通过接口映射关联接口方法与实现方法，保证显式接口实现和实现方法输入都能读取对应方法特性。
/// </remarks>
public interface IInterceptorSelector
{
    /// <summary>
    /// 选择指定服务方法需要执行的拦截器类型
    /// </summary>
    /// <param name="implementationType">服务实现类型</param>
    /// <param name="serviceType">服务契约类型</param>
    /// <param name="method">被调用的方法，可能来自服务契约或实现类型</param>
    /// <returns>按执行顺序排列的拦截器类型列表；方法或类型禁用拦截时返回空列表</returns>
    /// <exception cref="ArgumentNullException">implementationType、serviceType 或 method 为 null 时抛出</exception>
    IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, MethodInfo method);
}
