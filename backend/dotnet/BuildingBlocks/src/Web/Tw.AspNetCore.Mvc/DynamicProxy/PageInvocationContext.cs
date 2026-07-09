using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Tw.Castle.Core.Abstractions;

namespace Tw.AspNetCore.Mvc.DynamicProxy;

/// <summary>
/// 将 Razor Page handler filter 调用适配为统一方法级调用上下文
/// </summary>
public sealed class PageInvocationContext : IInvocationContext
{
    /// <summary>表示 _context 字段</summary>
    private readonly PageHandlerExecutingContext _context;
    /// <summary>表示 _next 字段</summary>
    private readonly PageHandlerExecutionDelegate _next;
    /// <summary>表示 _parameterNames 字段</summary>
    private readonly string[] _parameterNames;
    /// <summary>表示 _executedContext 字段</summary>
    private PageHandlerExecutedContext? _executedContext;
    /// <summary>表示 _returnValue 字段</summary>
    private object? _returnValue;

    /// <summary>
    /// 使用当前 Razor Page handler 执行上下文和后续执行委托创建调用上下文
    /// </summary>
    /// <param name="context">Razor Page handler 执行前上下文</param>
    /// <param name="next">继续执行 Razor Page handler 管线的委托</param>
    /// <exception cref="ArgumentNullException">context 或 next 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">无法从 handler 描述符建立完整方法或参数映射时抛出</exception>
    public PageInvocationContext(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        _context = context;
        _next = next;
        Method = ResolveMethod(context);
        Target = context.HandlerInstance;
        _parameterNames = ResolveParameterNames(Method, context);
        Arguments = CreateArguments(context, _parameterNames);
        ArgumentsByName = new ReadOnlyDictionary<string, object?>(
            _parameterNames
                .Select((name, index) => new KeyValuePair<string, object?>(name, Arguments[index]))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    }

    /// <inheritdoc />
    public MethodInfo Method { get; }

    /// <inheritdoc />
    public object? Target { get; }

    /// <inheritdoc />
    public object?[] Arguments { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> ArgumentsByName { get; }

    /// <inheritdoc />
    public object? ReturnValue
    {
        get => _returnValue;
        set
        {
            _returnValue = value;
            if (value is IActionResult actionResult)
            {
                if (_executedContext is null)
                {
                    _context.Result = actionResult;
                }
                else
                {
                    _executedContext.Result = actionResult;
                    if (_executedContext.Exception is not null)
                    {
                        _executedContext.ExceptionHandled = true;
                    }

                    _context.Result = actionResult;
                }
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask ProceedAsync()
    {
        WriteArgumentsToHandlerContext();

        var executedContext = await _next().ConfigureAwait(false);
        _executedContext = executedContext;

        if (executedContext.Exception is not null && !executedContext.ExceptionHandled)
        {
            ExceptionDispatchInfo.Capture(executedContext.Exception).Throw();
        }

        if (executedContext.Result is not null)
        {
            ReturnValue = executedContext.Result;
        }
    }

    /// <inheritdoc />
    public void Proceed() =>
        throw new InvalidOperationException("Razor Page handler filter 是异步上下文，请调用 ProceedAsync");

    /// <summary>执行 ResolveMethod 操作</summary>
    /// <param name="context">context 参数</param>
    /// <returns>ResolveMethod 的执行结果</returns>
    private static MethodInfo ResolveMethod(PageHandlerExecutingContext context) =>
        context.HandlerMethod?.MethodInfo
        ?? throw new InvalidOperationException(
            $"Razor Page '{ResolveHandlerName(context)}' 缺少 handler MethodInfo，无法建立调用上下文");

    /// <summary>执行 ResolveParameterNames 操作</summary>
    /// <param name="method">method 参数</param>
    /// <param name="context">context 参数</param>
    /// <returns>ResolveParameterNames 的执行结果</returns>
    private static string[] ResolveParameterNames(MethodInfo method, PageHandlerExecutingContext context)
    {
        var parameters = method.GetParameters();
        var parameterNames = new string[parameters.Length];

        for (var index = 0; index < parameters.Length; index++)
        {
            var parameterName = parameters[index].Name;
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new InvalidOperationException(
                    $"Razor Page handler '{ResolveHandlerName(context, method)}' 无法建立参数名映射，缺失参数名 '<parameter:{index}>'");
            }

            parameterNames[index] = parameterName;
        }

        return parameterNames;
    }

    /// <summary>执行 CreateArguments 操作</summary>
    /// <param name="context">context 参数</param>
    /// <param name="parameterNames">parameterNames 参数</param>
    /// <returns>CreateArguments 的执行结果</returns>
    private static object?[] CreateArguments(PageHandlerExecutingContext context, IReadOnlyList<string> parameterNames)
    {
        var arguments = new object?[parameterNames.Count];
        for (var index = 0; index < parameterNames.Count; index++)
        {
            var parameterName = parameterNames[index];
            if (!context.HandlerArguments.ContainsKey(parameterName))
            {
                throw MissingArgumentMapping(context, parameterName);
            }

            arguments[index] = context.HandlerArguments[parameterName];
        }

        return arguments;
    }

    /// <summary>执行 MissingArgumentMapping 操作</summary>
    /// <param name="context">context 参数</param>
    /// <param name="parameterName">parameterName 参数</param>
    /// <returns>MissingArgumentMapping 的执行结果</returns>
    private static InvalidOperationException MissingArgumentMapping(
        PageHandlerExecutingContext context,
        string parameterName) =>
        new($"Razor Page handler '{ResolveHandlerName(context)}' 无法建立参数映射，缺失参数名 '{parameterName}'");

    /// <summary>执行 ResolveHandlerName 操作</summary>
    /// <param name="context">context 参数</param>
    /// <param name="method">method 参数</param>
    /// <returns>ResolveHandlerName 的执行结果</returns>
    private static string ResolveHandlerName(PageHandlerExecutingContext context, MethodInfo? method = null)
    {
        if (context.HandlerMethod is { Name.Length: > 0 } handlerMethod)
        {
            return handlerMethod.Name;
        }

        return method?.Name
            ?? context.ActionDescriptor.DisplayName
            ?? "<unknown>";
    }

    /// <summary>执行 WriteArgumentsToHandlerContext 操作</summary>
    private void WriteArgumentsToHandlerContext()
    {
        for (var index = 0; index < _parameterNames.Length; index++)
        {
            var parameterName = _parameterNames[index];
            if (!_context.HandlerArguments.ContainsKey(parameterName))
            {
                throw MissingArgumentMapping(_context, parameterName);
            }

            _context.HandlerArguments[parameterName] = Arguments[index];
        }
    }
}
