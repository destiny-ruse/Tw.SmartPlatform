using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;

namespace Tw.AspNetCore.Mvc.DynamicProxy;

/// <summary>
/// 将 MVC action 执行接入统一动态代理拦截器管道
/// </summary>
public sealed class TwActionInterceptionFilter : IAsyncActionFilter
{
    /// <summary>表示 _serviceProvider 字段</summary>
    private readonly IServiceProvider _serviceProvider;
    /// <summary>表示 _selector 字段</summary>
    private readonly IInterceptorSelector _selector;

    /// <summary>
    /// 创建 MVC action 拦截 filter
    /// </summary>
    /// <param name="serviceProvider">用于解析拦截器实例的服务提供器</param>
    /// <param name="selector">根据当前 action 选择拦截器类型的选择器</param>
    /// <exception cref="ArgumentNullException">serviceProvider 或 selector 为 null 时抛出</exception>
    public TwActionInterceptionFilter(
        IServiceProvider serviceProvider,
        IInterceptorSelector selector)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(selector);

        _serviceProvider = serviceProvider;
        _selector = selector;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var method = ResolveMethod(context);
        var implementationType = ResolveImplementationType(context, method);
        var interceptorTypes = _selector.SelectInterceptors(
            implementationType,
            implementationType,
            method);

        if (interceptorTypes.Count == 0)
        {
            await next().ConfigureAwait(false);

            return;
        }

        ValidateInterceptorTypes(interceptorTypes);
        var interceptors = ResolveInterceptors(interceptorTypes);
        var pipeline = _serviceProvider.GetRequiredService<IInterceptorPipeline>();
        var invocationContext = new MvcInvocationContext(context, next);
        await pipeline.InvokeAsync(invocationContext, interceptors).ConfigureAwait(false);
    }

    /// <summary>执行 ResolveMethod 操作</summary>
    /// <param name="context">context 参数</param>
    /// <returns>ResolveMethod 的执行结果</returns>
    private static MethodInfo ResolveMethod(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
        {
            throw new InvalidOperationException(
                $"ControllerActionDescriptor 是 MVC action '{ResolveActionName(context)}' 选择拦截器的必要条件");
        }

        return actionDescriptor.MethodInfo
            ?? throw new InvalidOperationException(
                $"MVC action '{ResolveActionName(context)}' 缺少 MethodInfo，无法选择拦截器");
    }

    /// <summary>执行 ResolveImplementationType 操作</summary>
    /// <param name="context">context 参数</param>
    /// <param name="method">method 参数</param>
    /// <returns>ResolveImplementationType 的执行结果</returns>
    private static Type ResolveImplementationType(ActionExecutingContext context, MethodInfo method) =>
        context.Controller?.GetType()
        ?? method.DeclaringType
        ?? throw new InvalidOperationException(
            $"MVC action '{method.Name}' 无法解析控制器实现类型，不能选择拦截器");

    /// <summary>执行 ResolveActionName 操作</summary>
    /// <param name="context">context 参数</param>
    /// <returns>ResolveActionName 的执行结果</returns>
    private static string ResolveActionName(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor { ActionName.Length: > 0 } actionDescriptor)
        {
            return actionDescriptor.ActionName;
        }

        return context.ActionDescriptor.DisplayName ?? "<unknown>";
    }

    /// <summary>执行 ResolveInterceptors 操作</summary>
    /// <param name="interceptorTypes">interceptorTypes 参数</param>
    /// <returns>ResolveInterceptors 的执行结果</returns>
    private IReadOnlyList<IInterceptor> ResolveInterceptors(IReadOnlyList<Type> interceptorTypes)
    {
        var interceptors = new List<IInterceptor>(interceptorTypes.Count);
        foreach (var interceptorType in interceptorTypes)
        {
            var interceptor = _serviceProvider.GetRequiredService(interceptorType);
            if (interceptor is not IInterceptor typedInterceptor)
            {
                throw new InvalidOperationException(
                    $"拦截器类型 {interceptorType.FullName ?? interceptorType.Name} 必须实现 {typeof(IInterceptor).FullName}");
            }

            interceptors.Add(typedInterceptor);
        }

        return interceptors;
    }

    /// <summary>执行 ValidateInterceptorTypes 操作</summary>
    /// <param name="interceptorTypes">interceptorTypes 参数</param>
    private static void ValidateInterceptorTypes(IEnumerable<Type> interceptorTypes)
    {
        foreach (var interceptorType in interceptorTypes)
        {
            if (!typeof(IInterceptor).IsAssignableFrom(interceptorType))
            {
                throw new InvalidOperationException(
                    $"拦截器类型 {interceptorType.FullName ?? interceptorType.Name} 必须实现 {typeof(IInterceptor).FullName}");
            }
        }
    }
}
