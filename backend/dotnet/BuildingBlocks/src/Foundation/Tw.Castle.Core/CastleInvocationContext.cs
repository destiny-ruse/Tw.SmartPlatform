using System.Reflection;
using Tw.Castle.Core.Abstractions;
using CastleInvocation = Castle.DynamicProxy.IInvocation;

namespace Tw.Castle.Core;

/// <summary>
/// 将 Castle DynamicProxy 调用适配为统一方法级调用上下文
/// </summary>
public sealed class CastleInvocationContext : IInvocationContext
{
    /// <summary>表示 AwaitValueTaskWithResultMethod 字段</summary>
    private static readonly MethodInfo AwaitValueTaskWithResultMethod = typeof(CastleInvocationContext)
        .GetMethod(nameof(AwaitValueTaskWithResultAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>表示 CreateTaskWithResultMethod 字段</summary>
    private static readonly MethodInfo CreateTaskWithResultMethod = typeof(CastleInvocationContext)
        .GetMethod(nameof(CreateTaskWithResult), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>表示 CreateValueTaskWithResultMethod 字段</summary>
    private static readonly MethodInfo CreateValueTaskWithResultMethod = typeof(CastleInvocationContext)
        .GetMethod(nameof(CreateValueTaskWithResult), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>表示 _invocation 字段</summary>
    private readonly CastleInvocation _invocation;
    /// <summary>表示 _returnValue 字段</summary>
    private object? _returnValue;

    /// <summary>
    /// 使用 Castle 调用对象创建上下文
    /// </summary>
    /// <param name="invocation">Castle DynamicProxy 当前调用对象</param>
    /// <exception cref="ArgumentNullException">invocation 为 null 时抛出</exception>
    public CastleInvocationContext(CastleInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        _invocation = invocation;
        Method = invocation.MethodInvocationTarget ?? invocation.Method;
        Target = invocation.InvocationTarget;
        Arguments = invocation.Arguments.ToArray();
        ArgumentsByName = CreateArgumentsByName(Method, Arguments);
        _returnValue = invocation.ReturnValue;
    }

    /// <inheritdoc />
    public MethodInfo Method { get; }

    /// <inheritdoc />
    public object? Target { get; }

    /// <inheritdoc />
    public object?[] Arguments { get; }

    /// <summary>
    /// 按构造时参数名和参数值建立的只读快照，不随 <see cref="Arguments"/> 后续改写变化
    /// </summary>
    public IReadOnlyDictionary<string, object?> ArgumentsByName { get; }

    /// <inheritdoc />
    public object? ReturnValue
    {
        get => _returnValue;
        set
        {
            _returnValue = value;
            ApplyReturnValueToInvocation();
        }
    }

    /// <inheritdoc />
    public async ValueTask ProceedAsync()
    {
        WriteArgumentsToInvocation();

        _invocation.Proceed();
        _returnValue = await ReadCompletedReturnValueAsync(_invocation.ReturnValue, Method.ReturnType)
            .ConfigureAwait(false);
        ApplyReturnValueToInvocation();
    }

    /// <inheritdoc />
    public void Proceed()
    {
        if (IsAsyncReturnType(Method.ReturnType))
        {
            throw new InvalidOperationException("异步目标方法不能使用同步 Proceed，请调用 ProceedAsync");
        }

        WriteArgumentsToInvocation();

        _invocation.Proceed();
        ReturnValue = _invocation.ReturnValue;
    }

    /// <summary>
    /// 将当前逻辑返回值按目标方法签名包装后写回 Castle invocation
    /// </summary>
    internal void ApplyReturnValueToInvocation() =>
        _invocation.ReturnValue = CreateCompatibleReturnValue(Method.ReturnType, _returnValue);

    /// <summary>执行 CreateArgumentsByName 操作</summary>
    /// <param name="method">method 参数</param>
    /// <param name="arguments">arguments 参数</param>
    /// <returns>CreateArgumentsByName 的执行结果</returns>
    private static IReadOnlyDictionary<string, object?> CreateArgumentsByName(MethodInfo method, object?[] arguments)
    {
        var parameters = method.GetParameters();
        var argumentsByName = new Dictionary<string, object?>(StringComparer.Ordinal);
        var argumentCount = Math.Min(parameters.Length, arguments.Length);

        for (var index = 0; index < argumentCount; index++)
        {
            var parameterName = parameters[index].Name;
            if (!string.IsNullOrEmpty(parameterName))
            {
                argumentsByName[parameterName] = arguments[index];
            }
        }

        return argumentsByName;
    }

    /// <summary>执行 ReadCompletedReturnValueAsync 操作</summary>
    /// <param name="returnValue">returnValue 参数</param>
    /// <param name="returnType">returnType 参数</param>
    /// <returns>ReadCompletedReturnValueAsync 的执行结果</returns>
    private static async ValueTask<object?> ReadCompletedReturnValueAsync(object? returnValue, Type returnType)
    {
        if (returnValue is null)
        {
            return null;
        }

        if (returnValue is Task task)
        {
            await task.ConfigureAwait(false);

            return ReadTaskResult(task, returnType);
        }

        if (returnType == typeof(ValueTask))
        {
            await ((ValueTask)returnValue).ConfigureAwait(false);

            return null;
        }

        if (IsValueTaskWithResult(returnType))
        {
            var awaitResult = (ValueTask<object?>)AwaitValueTaskWithResultMethod
                .MakeGenericMethod(returnType.GenericTypeArguments[0])
                .Invoke(null, [returnValue])!;

            return await awaitResult.ConfigureAwait(false);
        }

        return returnValue;
    }

    /// <summary>执行 ReadTaskResult 操作</summary>
    /// <param name="task">task 参数</param>
    /// <param name="returnType">returnType 参数</param>
    /// <returns>ReadTaskResult 的执行结果</returns>
    private static object? ReadTaskResult(Task task, Type returnType)
    {
        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            return null;
        }

        return returnType.GetProperty(nameof(Task<object>.Result))!.GetValue(task);
    }

    /// <summary>执行 AwaitValueTaskWithResultAsync 操作</summary>
    /// <typeparam name="TResult">TResult 类型参数</typeparam>
    /// <param name="valueTask">valueTask 参数</param>
    /// <returns>AwaitValueTaskWithResultAsync 的执行结果</returns>
    private static async ValueTask<object?> AwaitValueTaskWithResultAsync<TResult>(ValueTask<TResult> valueTask) =>
        await valueTask.ConfigureAwait(false);

    /// <summary>执行 CreateTaskWithResult 操作</summary>
    /// <typeparam name="TResult">TResult 类型参数</typeparam>
    /// <param name="returnValue">returnValue 参数</param>
    /// <returns>CreateTaskWithResult 的执行结果</returns>
    private static Task<TResult> CreateTaskWithResult<TResult>(object? returnValue) =>
        Task.FromResult(returnValue is null ? default! : (TResult)returnValue);

    /// <summary>执行 CreateValueTaskWithResult 操作</summary>
    /// <typeparam name="TResult">TResult 类型参数</typeparam>
    /// <param name="returnValue">returnValue 参数</param>
    /// <returns>CreateValueTaskWithResult 的执行结果</returns>
    private static ValueTask<TResult> CreateValueTaskWithResult<TResult>(object? returnValue) =>
        ValueTask.FromResult(returnValue is null ? default! : (TResult)returnValue);

    /// <summary>执行 CreateCompatibleReturnValue 操作</summary>
    /// <param name="returnType">returnType 参数</param>
    /// <param name="returnValue">returnValue 参数</param>
    /// <returns>CreateCompatibleReturnValue 的执行结果</returns>
    private static object? CreateCompatibleReturnValue(Type returnType, object? returnValue)
    {
        if (returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return CreateTaskWithResultMethod
                .MakeGenericMethod(returnType.GenericTypeArguments[0])
                .Invoke(null, [returnValue]);
        }

        if (returnType == typeof(ValueTask))
        {
            return ValueTask.CompletedTask;
        }

        if (IsValueTaskWithResult(returnType))
        {
            return CreateValueTaskWithResultMethod
                .MakeGenericMethod(returnType.GenericTypeArguments[0])
                .Invoke(null, [returnValue]);
        }

        return returnValue;
    }

    /// <summary>执行 IsAsyncReturnType 操作</summary>
    /// <param name="returnType">returnType 参数</param>
    /// <returns>IsAsyncReturnType 的执行结果</returns>
    private static bool IsAsyncReturnType(Type returnType) =>
        typeof(Task).IsAssignableFrom(returnType)
        || returnType == typeof(ValueTask)
        || IsValueTaskWithResult(returnType);

    /// <summary>执行 IsValueTaskWithResult 操作</summary>
    /// <param name="returnType">returnType 参数</param>
    /// <returns>IsValueTaskWithResult 的执行结果</returns>
    private static bool IsValueTaskWithResult(Type returnType) =>
        returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>);

    /// <summary>执行 WriteArgumentsToInvocation 操作</summary>
    private void WriteArgumentsToInvocation()
    {
        for (var index = 0; index < Arguments.Length; index++)
        {
            _invocation.SetArgumentValue(index, Arguments[index]);
        }
    }
}
