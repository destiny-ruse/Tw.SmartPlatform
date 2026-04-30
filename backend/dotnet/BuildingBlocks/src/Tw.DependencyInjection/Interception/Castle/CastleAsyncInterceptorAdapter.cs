using System.Reflection;
using Autofac;
using Castle.DynamicProxy;
using Tw.Core.Exceptions;

namespace Tw.DependencyInjection.Interception.Castle;

/// <summary>
/// 将 Castle DynamicProxy 调用适配到 Tw 拦截器链
/// </summary>
public sealed class CastleAsyncInterceptorAdapter : IInterceptor, IAsyncInterceptor
{
    private readonly AsyncDeterminationInterceptor _asyncDeterminationInterceptor;
    private readonly ILifetimeScope _lifetimeScope;
    private readonly ServiceChainCache _serviceChainCache;

    /// <summary>
    /// 初始化 Castle 异步拦截适配器
    /// </summary>
    /// <param name="lifetimeScope">当前 Autofac 生命周期作用域</param>
    /// <param name="serviceChainCache">Service 作用域拦截器链缓存</param>
    public CastleAsyncInterceptorAdapter(
        ILifetimeScope lifetimeScope,
        ServiceChainCache serviceChainCache)
    {
        _lifetimeScope = lifetimeScope;
        _serviceChainCache = serviceChainCache;
        _asyncDeterminationInterceptor = new AsyncDeterminationInterceptor(this);
    }

    /// <inheritdoc />
    public void Intercept(IInvocation invocation)
    {
        if (IsValueTask(invocation.Method.ReturnType))
        {
            InterceptValueTask(invocation);
            return;
        }

        _asyncDeterminationInterceptor.Intercept(invocation);
    }

    /// <inheritdoc />
    public void InterceptSynchronous(IInvocation invocation)
    {
        var context = CreateContext(invocation);
        ExecuteChainAsync(invocation, context).GetAwaiter().GetResult();
        invocation.ReturnValue = ValidateAndGetReturnValue(context);
    }

    /// <inheritdoc />
    public void InterceptAsynchronous(IInvocation invocation)
        => invocation.ReturnValue = InterceptTaskAsync(invocation);

    /// <inheritdoc />
    public void InterceptAsynchronous<TResult>(IInvocation invocation)
        => invocation.ReturnValue = InterceptTaskWithResultAsync<TResult>(invocation);

    private async Task InterceptTaskAsync(IInvocation invocation)
    {
        var context = CreateContext(invocation);
        await ExecuteChainAsync(invocation, context);
        ValidateAndGetReturnValue(context);
    }

    private async Task<TResult> InterceptTaskWithResultAsync<TResult>(IInvocation invocation)
    {
        var context = CreateContext(invocation);
        await ExecuteChainAsync(invocation, context);
        return (TResult)ValidateAndGetReturnValue(context)!;
    }

    private void InterceptValueTask(IInvocation invocation)
    {
        var returnType = invocation.Method.ReturnType;
        if (returnType == typeof(ValueTask))
        {
            invocation.ReturnValue = InterceptValueTaskAsync(invocation);
            return;
        }

        var resultType = returnType.GetGenericArguments()[0];
        var method = typeof(CastleAsyncInterceptorAdapter)
            .GetMethod(nameof(InterceptValueTaskWithResultAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(resultType);
        invocation.ReturnValue = method.Invoke(this, [invocation]);
    }

    private async ValueTask InterceptValueTaskAsync(IInvocation invocation)
    {
        var context = CreateContext(invocation);
        await ExecuteChainAsync(invocation, context);
        ValidateAndGetReturnValue(context);
    }

    private async ValueTask<TResult> InterceptValueTaskWithResultAsync<TResult>(IInvocation invocation)
    {
        var context = CreateContext(invocation);
        await ExecuteChainAsync(invocation, context);
        return (TResult)ValidateAndGetReturnValue(context)!;
    }

    private CastleUnaryInvocationContext CreateContext(IInvocation invocation)
    {
        var method = invocation.MethodInvocationTarget ?? invocation.Method;
        return new CastleUnaryInvocationContext(
            method,
            invocation.Arguments,
            UnwrapReturnType(invocation.Method.ReturnType),
            ResolveCancellationToken(invocation.Arguments));
    }

    private async ValueTask ExecuteChainAsync(
        IInvocation invocation,
        CastleUnaryInvocationContext context)
    {
        var implementationType = invocation.TargetType
                                 ?? invocation.InvocationTarget?.GetType()
                                 ?? invocation.MethodInvocationTarget?.DeclaringType
                                 ?? invocation.Method.DeclaringType
                                 ?? throw new TwConfigurationException("无法确定 Castle 代理调用的目标实现类型");

        var chain = _serviceChainCache.GetInterceptors(implementationType);
        await InvokeAtAsync(0);
        ValidateAndGetReturnValue(context);

        async ValueTask InvokeAtAsync(int index)
        {
            if (index == chain.Count)
            {
                await ProceedTargetAsync(invocation, context);
                return;
            }

            var proceeded = false;
            context.SetProceed(async () =>
            {
                if (proceeded)
                {
                    throw new TwConfigurationException(
                        $"拦截器重复调用 ProceedAsync，目标方法：{FormatMethod(context.Method)}");
                }

                proceeded = true;
                await InvokeAtAsync(index + 1);
            });

            var interceptor = (InterceptorBase)_lifetimeScope.Resolve(chain[index].InterceptorType);
            await interceptor.InterceptAsync(context);
        }
    }

    private static async ValueTask ProceedTargetAsync(
        IInvocation invocation,
        CastleUnaryInvocationContext context)
    {
        invocation.Proceed();
        var returnType = invocation.Method.ReturnType;

        if (returnType == typeof(void))
        {
            context.ReturnValue = null;
            return;
        }

        if (returnType == typeof(Task))
        {
            await (Task)invocation.ReturnValue!;
            context.ReturnValue = null;
            return;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var task = (Task)invocation.ReturnValue!;
            await task;
            context.ReturnValue = task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
            return;
        }

        if (returnType == typeof(ValueTask))
        {
            await (ValueTask)invocation.ReturnValue!;
            context.ReturnValue = null;
            return;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var valueTask = invocation.ReturnValue!;
            var task = (Task)valueTask.GetType().GetMethod(nameof(ValueTask<object>.AsTask))!.Invoke(valueTask, null)!;
            await task;
            context.ReturnValue = task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
            return;
        }

        context.ReturnValue = invocation.ReturnValue;
    }

    private object? ValidateAndGetReturnValue(CastleUnaryInvocationContext context)
    {
        if (context.ReturnType == typeof(void))
        {
            return null;
        }

        var value = context.ReturnValue;
        if (value is null)
        {
            if (context.ReturnType.IsValueType && Nullable.GetUnderlyingType(context.ReturnType) is null)
            {
                ThrowInvalidReturnValue(context);
            }

            return null;
        }

        if (!context.ReturnType.IsInstanceOfType(value))
        {
            ThrowInvalidReturnValue(context);
        }

        return value;
    }

    private static void ThrowInvalidReturnValue(CastleUnaryInvocationContext context)
        => throw new TwConfigurationException(
            $"拦截器返回值类型不匹配，目标方法：{FormatMethod(context.Method)}，期望返回类型：{context.ReturnType.FullName}");

    private static Type UnwrapReturnType(Type returnType)
    {
        if (returnType == typeof(void) || returnType == typeof(Task) || returnType == typeof(ValueTask))
        {
            return typeof(void);
        }

        if (returnType.IsGenericType)
        {
            var definition = returnType.GetGenericTypeDefinition();
            if (definition == typeof(Task<>) || definition == typeof(ValueTask<>))
            {
                return returnType.GetGenericArguments()[0];
            }
        }

        return returnType;
    }

    private static bool IsValueTask(Type returnType)
        => returnType == typeof(ValueTask)
           || (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>));

    private static CancellationToken ResolveCancellationToken(object?[] arguments)
        => arguments.OfType<CancellationToken>().FirstOrDefault();

    private static string FormatMethod(MethodInfo method)
        => $"{method.DeclaringType?.FullName ?? "<unknown>"}.{method.Name}";
}
