using System.Reflection;
using Tw.DynamicProxy.Abstractions;
using CastleInvocation = Castle.DynamicProxy.IInvocation;

namespace Tw.DependencyInjection.DynamicProxy;

/// <summary>
/// 将 Castle DynamicProxy 调用适配为统一方法级调用上下文
/// </summary>
public sealed class CastleInvocationContext : IInvocationContext
{
    private static readonly MethodInfo AwaitValueTaskWithResultMethod = typeof(CastleInvocationContext)
        .GetMethod(nameof(AwaitValueTaskWithResultAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly CastleInvocation _invocation;
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

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> ArgumentsByName { get; }

    /// <inheritdoc />
    public object? ReturnValue
    {
        get => _returnValue;
        set
        {
            _returnValue = value;
            _invocation.ReturnValue = value;
        }
    }

    /// <inheritdoc />
    public async ValueTask ProceedAsync()
    {
        WriteArgumentsToInvocation();

        _invocation.Proceed();
        ReturnValue = await ReadCompletedReturnValueAsync(_invocation.ReturnValue, Method.ReturnType)
            .ConfigureAwait(false);
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

    private static object? ReadTaskResult(Task task, Type returnType)
    {
        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            return null;
        }

        return returnType.GetProperty(nameof(Task<object>.Result))!.GetValue(task);
    }

    private static async ValueTask<object?> AwaitValueTaskWithResultAsync<TResult>(ValueTask<TResult> valueTask) =>
        await valueTask.ConfigureAwait(false);

    private static bool IsAsyncReturnType(Type returnType) =>
        typeof(Task).IsAssignableFrom(returnType)
        || returnType == typeof(ValueTask)
        || IsValueTaskWithResult(returnType);

    private static bool IsValueTaskWithResult(Type returnType) =>
        returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>);

    private void WriteArgumentsToInvocation()
    {
        for (var index = 0; index < Arguments.Length; index++)
        {
            _invocation.SetArgumentValue(index, Arguments[index]);
        }
    }
}
