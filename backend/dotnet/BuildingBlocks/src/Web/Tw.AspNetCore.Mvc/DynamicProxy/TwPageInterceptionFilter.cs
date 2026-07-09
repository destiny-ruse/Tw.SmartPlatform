using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;

namespace Tw.AspNetCore.Mvc.DynamicProxy;

/// <summary>
/// 将 Razor Page handler 执行接入统一动态代理拦截器管道
/// </summary>
public sealed class TwPageInterceptionFilter : IAsyncPageFilter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInterceptorSelector _selector;

    /// <summary>
    /// 创建 Razor Page handler 拦截 filter
    /// </summary>
    /// <param name="serviceProvider">用于解析拦截器实例的服务提供器</param>
    /// <param name="selector">根据当前 handler 选择拦截器类型的选择器</param>
    /// <exception cref="ArgumentNullException">serviceProvider 或 selector 为 null 时抛出</exception>
    public TwPageInterceptionFilter(
        IServiceProvider serviceProvider,
        IInterceptorSelector selector)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(selector);

        _serviceProvider = serviceProvider;
        _selector = selector;
    }

    /// <inheritdoc />
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var method = context.HandlerMethod?.MethodInfo;
        if (method is null)
        {
            // 请求未命中具名 handler（例如页面没有对应 OnGet/OnPost），无方法级调用可拦截
            await next().ConfigureAwait(false);

            return;
        }

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
        var invocationContext = new PageInvocationContext(context, next);
        await pipeline.InvokeAsync(invocationContext, interceptors).ConfigureAwait(false);
    }

    private static Type ResolveImplementationType(PageHandlerExecutingContext context, MethodInfo method) =>
        context.HandlerInstance?.GetType()
        ?? method.DeclaringType
        ?? throw new InvalidOperationException(
            $"Razor Page handler '{method.Name}' 无法解析 page model 实现类型，不能选择拦截器");

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
