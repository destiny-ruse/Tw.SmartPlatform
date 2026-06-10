using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Diagnostics;
using AbstractionInterceptor = Tw.DynamicProxy.Abstractions.IInterceptor;
using CastleAsyncInterceptor = Castle.DynamicProxy.IAsyncInterceptor;
using CastleInterceptor = Castle.DynamicProxy.IInterceptor;
using CastleInvocation = Castle.DynamicProxy.IInvocation;

namespace Tw.DependencyInjection.DynamicProxy;

/// <summary>
/// 将 Castle.Core.AsyncInterceptor 调用适配到统一拦截器管道
/// </summary>
/// <remarks>
/// 当选择器未返回拦截器类型时直接推进 Castle 调用；存在拦截器时通过 <see cref="IInterceptorPipeline"/> 执行统一调用链
/// Castle 会把同步方法以及 <see cref="ValueTask"/>、<see cref="ValueTask{TResult}"/> 方法分派到同步入口，
/// 同步入口会阻塞等待统一 pipeline 完成，拦截器实现不应依赖捕获同步上下文恢复
/// </remarks>
public sealed class CastleAsyncInterceptorAdapter : CastleAsyncInterceptor, CastleInterceptor
{
    private const string Enabled = "enabled";
    private const string CastleInterfaceProxy = "CastleInterfaceProxy";

    private static readonly MethodInfo InterceptAsynchronousWithResultMethod = typeof(CastleAsyncInterceptorAdapter)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(method => method.Name == nameof(InterceptAsynchronous) && method.IsGenericMethodDefinition);

    private readonly IInterceptorSelector _selector;
    private readonly IInterceptorPipeline _pipeline;
    private readonly IServiceProvider _serviceProvider;
    private readonly InterceptionReport? _interceptionReport;

    /// <summary>
    /// 创建 Castle 异步拦截器适配器
    /// </summary>
    /// <param name="selector">根据当前方法选择拦截器类型的选择器</param>
    /// <param name="pipeline">执行统一拦截器链的管道</param>
    /// <param name="serviceProvider">解析拦截器实例的服务提供器</param>
    /// <exception cref="ArgumentNullException">selector、pipeline 或 serviceProvider 为 null 时抛出</exception>
    public CastleAsyncInterceptorAdapter(
        IInterceptorSelector selector,
        IInterceptorPipeline pipeline,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _selector = selector;
        _pipeline = pipeline;
        _serviceProvider = serviceProvider;
        _interceptionReport = serviceProvider.GetService<InterceptionReport>();
    }

    /// <summary>
    /// 拦截 Castle DynamicProxy 的通用同步入口，并按目标方法返回类型分派到异步适配入口
    /// </summary>
    /// <param name="invocation">Castle 当前调用对象</param>
    /// <exception cref="ArgumentNullException">invocation 为 null 时抛出</exception>
    public void Intercept(CastleInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        var returnType = ResolveMethod(invocation).ReturnType;
        if (returnType == typeof(Task))
        {
            InterceptAsynchronous(invocation);
            return;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            try
            {
                InterceptAsynchronousWithResultMethod
                    .MakeGenericMethod(returnType.GenericTypeArguments[0])
                    .Invoke(this, [invocation]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }

            return;
        }

        InterceptSynchronous(invocation);
    }

    /// <summary>
    /// 拦截 Castle 同步方法调用
    /// </summary>
    /// <param name="invocation">Castle 当前调用对象</param>
    /// <exception cref="ArgumentNullException">invocation 为 null 时抛出</exception>
    public void InterceptSynchronous(CastleInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        var interceptors = ResolveInterceptors(invocation);
        if (interceptors.Count == 0)
        {
            invocation.Proceed();

            return;
        }

        var context = new CastleInvocationContext(invocation);
        _pipeline.InvokeAsync(context, interceptors).AsTask().GetAwaiter().GetResult();
        context.ApplyReturnValueToInvocation();
    }

    /// <summary>
    /// 拦截返回 <see cref="Task"/> 的 Castle 异步方法调用
    /// </summary>
    /// <param name="invocation">Castle 当前调用对象</param>
    /// <exception cref="ArgumentNullException">invocation 为 null 时抛出</exception>
    public void InterceptAsynchronous(CastleInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        var interceptors = ResolveInterceptors(invocation);
        if (interceptors.Count == 0)
        {
            invocation.Proceed();

            return;
        }

        invocation.ReturnValue = InvokePipelineAsTaskAsync(invocation, interceptors);
    }

    /// <summary>
    /// 拦截返回 <see cref="Task{TResult}"/> 的 Castle 异步方法调用
    /// </summary>
    /// <typeparam name="TResult">异步方法结果类型</typeparam>
    /// <param name="invocation">Castle 当前调用对象</param>
    /// <exception cref="ArgumentNullException">invocation 为 null 时抛出</exception>
    public void InterceptAsynchronous<TResult>(CastleInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        var interceptors = ResolveInterceptors(invocation);
        if (interceptors.Count == 0)
        {
            invocation.Proceed();

            return;
        }

        invocation.ReturnValue = InvokePipelineAsTaskAsync<TResult>(invocation, interceptors);
    }

    private static MethodInfo ResolveMethod(CastleInvocation invocation) =>
        invocation.MethodInvocationTarget ?? invocation.Method;

    private static Type ResolveImplementationType(CastleInvocation invocation)
    {
        return invocation.MethodInvocationTarget?.DeclaringType
            ?? invocation.InvocationTarget?.GetType()
            ?? invocation.Method.DeclaringType
            ?? throw new InvalidOperationException("无法从 Castle invocation 解析实现类型");
    }

    private Type ResolveServiceType(CastleInvocation invocation, Type implementationType, MethodInfo method)
    {
        var declaredServiceType = invocation.Method.DeclaringType
            ?? invocation.MethodInvocationTarget?.DeclaringType
            ?? implementationType;

        if (!declaredServiceType.IsInterface)
        {
            return declaredServiceType;
        }

        return ResolveMostSpecificServiceInterface(invocation, implementationType, declaredServiceType, method, _interceptionReport)
            ?? declaredServiceType;
    }

    private static Type? ResolveMostSpecificServiceInterface(
        CastleInvocation invocation,
        Type implementationType,
        Type declaredServiceType,
        MethodInfo method,
        InterceptionReport? report)
    {
        var candidateInterfaces = (invocation.Proxy?.GetType().GetInterfaces() ?? [])
            .Where(declaredServiceType.IsAssignableFrom)
            .Distinct()
            .ToList();

        var mostSpecificInterfaces = candidateInterfaces
            .Where(candidate => !candidateInterfaces.Any(other =>
                candidate != other && candidate.IsAssignableFrom(other)))
            .ToList();

        var reportedServiceTypeNames = ResolveReportedServiceTypeNames(report, implementationType, method);
        if (reportedServiceTypeNames.Count > 0)
        {
            var reportedProxyInterfaces = mostSpecificInterfaces
                .Where(candidate => TypeNames(candidate).Any(reportedServiceTypeNames.Contains))
                .ToList();

            if (reportedProxyInterfaces.Count == 1)
            {
                return reportedProxyInterfaces[0];
            }

            var reportedImplementationInterfaces = implementationType
                .GetInterfaces()
                .Where(declaredServiceType.IsAssignableFrom)
                .Where(candidate => TypeNames(candidate).Any(reportedServiceTypeNames.Contains))
                .Distinct()
                .ToList();

            var mostSpecificReportedInterfaces = reportedImplementationInterfaces
                .Where(candidate => !reportedImplementationInterfaces.Any(other =>
                    candidate != other && candidate.IsAssignableFrom(other)))
                .ToList();

            if (mostSpecificReportedInterfaces.Count == 1)
            {
                return mostSpecificReportedInterfaces[0];
            }
        }

        return mostSpecificInterfaces.Count == 1 ? mostSpecificInterfaces[0] : null;
    }

    private static HashSet<string> ResolveReportedServiceTypeNames(
        InterceptionReport? report,
        Type implementationType,
        MethodInfo method)
    {
        if (report is null)
        {
            return [];
        }

        var implementationTypeNames = TypeNames(implementationType);
        return report.Items
            .Where(item => item.Status == Enabled
                && item.Carrier == CastleInterfaceProxy
                && item.MethodName == method.Name
                && implementationTypeNames.Contains(item.ImplementationTypeName))
            .Select(item => item.ServiceTypeName)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> TypeNames(Type type)
    {
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            TypeName(type),
        };

        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            names.Add(TypeName(type.GetGenericTypeDefinition()));
        }

        return names;
    }

    private IReadOnlyList<AbstractionInterceptor> ResolveInterceptors(CastleInvocation invocation)
    {
        var method = ResolveMethod(invocation);
        var implementationType = ResolveImplementationType(invocation);
        var serviceType = ResolveServiceType(invocation, implementationType, method);
        var interceptorTypes = _selector.SelectInterceptors(implementationType, serviceType, method);

        if (interceptorTypes.Count == 0)
        {
            return [];
        }

        var interceptors = new List<AbstractionInterceptor>(interceptorTypes.Count);
        foreach (var interceptorType in interceptorTypes)
        {
            var interceptor = _serviceProvider.GetRequiredService(interceptorType);
            if (interceptor is not AbstractionInterceptor typedInterceptor)
            {
                throw new InvalidOperationException(
                    $"拦截器类型 {interceptorType.FullName ?? interceptorType.Name} 必须实现 {typeof(AbstractionInterceptor).FullName}");
            }

            interceptors.Add(typedInterceptor);
        }

        return interceptors;
    }

    private async Task InvokePipelineAsTaskAsync(
        CastleInvocation invocation,
        IReadOnlyList<AbstractionInterceptor> interceptors)
    {
        var context = new CastleInvocationContext(invocation);

        await _pipeline.InvokeAsync(context, interceptors).ConfigureAwait(false);
    }

    private async Task<TResult> InvokePipelineAsTaskAsync<TResult>(
        CastleInvocation invocation,
        IReadOnlyList<AbstractionInterceptor> interceptors)
    {
        var context = new CastleInvocationContext(invocation);

        await _pipeline.InvokeAsync(context, interceptors).ConfigureAwait(false);

        if (context.ReturnValue is null)
        {
            return default!;
        }

        return (TResult)context.ReturnValue;
    }

    private static string TypeName(Type type) => type.FullName ?? type.Name;
}
