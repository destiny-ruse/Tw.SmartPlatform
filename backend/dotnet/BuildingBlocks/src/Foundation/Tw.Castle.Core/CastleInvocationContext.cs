using System.Reflection;
using Tw.Castle.Core.Abstractions;
using CastleInvocation = Castle.DynamicProxy.IInvocation;

namespace Tw.Castle.Core;

/// <summary>
/// 将 Castle DynamicProxy 调用适配为统一方法级调用上下文
/// </summary>
public sealed class CastleInvocationContext : IInvocationContext
{
    /// <summary>
    /// 保存当前类型处理流程依赖的Await值Task使用结果Method
    /// </summary>
    private static readonly MethodInfo AwaitValueTaskWithResultMethod = typeof(CastleInvocationContext)
        .GetMethod(nameof(AwaitValueTaskWithResultAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// 保存当前类型处理流程依赖的创建Task使用结果Method
    /// </summary>
    private static readonly MethodInfo CreateTaskWithResultMethod = typeof(CastleInvocationContext)
        .GetMethod(nameof(CreateTaskWithResult), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// 保存当前类型处理流程依赖的创建值Task使用结果Method
    /// </summary>
    private static readonly MethodInfo CreateValueTaskWithResultMethod = typeof(CastleInvocationContext)
        .GetMethod(nameof(CreateValueTaskWithResult), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// 保存当前类型处理流程依赖的调用
    /// </summary>
    private readonly CastleInvocation _invocation;
    /// <summary>
    /// 保存当前类型处理流程依赖的return值
    /// </summary>
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

    /// <summary>
    /// 创建参数By名称测试对象
    /// </summary>
    /// <param name="method">用于构造测试场景的方法元数据</param>
    /// <param name="arguments">用于提供arguments</param>
    /// <returns>方法计算得到的文本值</returns>
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

    /// <summary>
    /// 读取Completed返回值异步内容
    /// </summary>
    /// <param name="returnValue">用于提供return值</param>
    /// <param name="returnType">用于提供return类型</param>
    /// <returns>异步流程完成后产生的object</returns>
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

    /// <summary>
    /// 读取Task结果内容
    /// </summary>
    /// <param name="task">用于提供task</param>
    /// <param name="returnType">用于提供return类型</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    private static object? ReadTaskResult(Task task, Type returnType)
    {
        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            return null;
        }

        return returnType.GetProperty(nameof(Task<object>.Result))!.GetValue(task);
    }

    /// <summary>
    /// 说明Await值TaskWith结果Async在当前类型中的职责
    /// </summary>
    /// <typeparam name="TResult">响应数据的运行时类型</typeparam>
    /// <param name="valueTask">用于提供值Task</param>
    /// <returns>异步流程完成后产生的object</returns>
    private static async ValueTask<object?> AwaitValueTaskWithResultAsync<TResult>(ValueTask<TResult> valueTask) =>
        await valueTask.ConfigureAwait(false);

    /// <summary>
    /// 创建Task带有结果测试对象
    /// </summary>
    /// <typeparam name="TResult">响应数据的运行时类型</typeparam>
    /// <param name="returnValue">用于提供return值</param>
    /// <returns>异步流程完成后产生的T结果</returns>
    private static Task<TResult> CreateTaskWithResult<TResult>(object? returnValue) =>
        Task.FromResult(returnValue is null ? default! : (TResult)returnValue);

    /// <summary>
    /// 创建值Task带有结果测试对象
    /// </summary>
    /// <typeparam name="TResult">响应数据的运行时类型</typeparam>
    /// <param name="returnValue">用于提供return值</param>
    /// <returns>异步流程完成后产生的T结果</returns>
    private static ValueTask<TResult> CreateValueTaskWithResult<TResult>(object? returnValue) =>
        ValueTask.FromResult(returnValue is null ? default! : (TResult)returnValue);

    /// <summary>
    /// 创建Compatible返回值测试对象
    /// </summary>
    /// <param name="returnType">用于提供return类型</param>
    /// <param name="returnValue">用于提供return值</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
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

    /// <summary>
    /// 判断异步返回类型是否满足条件
    /// </summary>
    /// <param name="returnType">用于提供return类型</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsAsyncReturnType(Type returnType) =>
        typeof(Task).IsAssignableFrom(returnType)
        || returnType == typeof(ValueTask)
        || IsValueTaskWithResult(returnType);

    /// <summary>
    /// 判断值Task带有结果是否满足条件
    /// </summary>
    /// <param name="returnType">用于提供return类型</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsValueTaskWithResult(Type returnType) =>
        returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>);

    /// <summary>
    /// 说明写入ArgumentsToInvocation在当前类型中的职责
    /// </summary>
    private void WriteArgumentsToInvocation()
    {
        for (var index = 0; index < Arguments.Length; index++)
        {
            _invocation.SetArgumentValue(index, Arguments[index]);
        }
    }
}
