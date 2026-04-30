using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Tw.AspNetCore.Features;
using Tw.Core.Exceptions;
using Tw.DependencyInjection.Cancellation;
using Tw.DependencyInjection.Interception;

namespace Tw.AspNetCore.Mvc;

/// <summary>
/// 将 MVC Action 调用边界适配为 Tw Entry 拦截器链
/// </summary>
public sealed class TwEntryInterceptorFilter(
    EntryChainCache entryChainCache,
    ICurrentCancellationTokenAccessor cancellationTokenAccessor) : IAsyncActionFilter
{
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var controllerType = context.Controller.GetType();
        var chain = entryChainCache.GetInterceptors(controllerType);
        if (chain.Count == 0)
        {
            await next();
            return;
        }

        var method = ResolveMethod(context);
        var cancellationToken = ResolveCancellationToken(context);
        var invocationContext = new MvcUnaryInvocationContext(
            method,
            ResolveArguments(context),
            UnwrapReturnType(method.ReturnType),
            cancellationToken);
        invocationContext.Features.Set<IHttpRequestFeature>(
            new HttpRequestFeature(context.HttpContext, context));

        using var cancellationScope = cancellationTokenAccessor.Use(cancellationToken);
        ActionExecutedContext? executedContext = null;

        await InvokeAtAsync(0);

        if (executedContext is null && invocationContext.ReturnValue is IActionResult result)
        {
            context.Result = result;
        }

        async ValueTask InvokeAtAsync(int index)
        {
            if (index == chain.Count)
            {
                executedContext = await next();
                invocationContext.ReturnValue = executedContext.Result;
                return;
            }

            var proceeded = false;
            invocationContext.SetProceed(async () =>
            {
                if (proceeded)
                {
                    throw new TwConfigurationException(
                        $"Entry 拦截器重复调用 ProceedAsync，目标方法：{method.DeclaringType?.FullName}.{method.Name}");
                }

                proceeded = true;
                await InvokeAtAsync(index + 1);
            });

            var interceptor = (InterceptorBase)context.HttpContext.RequestServices.GetRequiredService(chain[index].InterceptorType);
            await interceptor.InterceptAsync(invocationContext);
        }
    }

    private static MethodInfo ResolveMethod(ActionExecutingContext context)
        => context.ActionDescriptor is ControllerActionDescriptor controllerAction
            ? controllerAction.MethodInfo
            : throw new TwConfigurationException("MVC Entry 拦截仅支持 ControllerActionDescriptor");

    private static object?[] ResolveArguments(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor controllerAction)
        {
            return [];
        }

        return controllerAction.Parameters
            .Select(p => context.ActionArguments.TryGetValue(p.Name!, out var value) ? value : null)
            .ToArray();
    }

    private static CancellationToken ResolveCancellationToken(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor controllerAction)
        {
            foreach (var parameter in controllerAction.Parameters.Where(p => p.ParameterType == typeof(CancellationToken)))
            {
                if (context.ActionArguments.TryGetValue(parameter.Name!, out var value) && value is CancellationToken token)
                {
                    return token;
                }
            }
        }

        return context.HttpContext.RequestAborted;
    }

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
}
