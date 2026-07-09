using System.Reflection;

namespace Tw.Castle.Core.Abstractions;

/// <summary>
/// 一次方法级调用的上下文，可适配 Castle invocation 与 MVC action
/// </summary>
public interface IInvocationContext
{
    /// <summary>被调用的目标方法</summary>
    MethodInfo Method { get; }

    /// <summary>调用目标实例，静态或不可用时为 <see langword="null"/></summary>
    object? Target { get; }

    /// <summary>按位置排列的调用参数，可在 Proceed 前改写以传递修改后的入参</summary>
    object?[] Arguments { get; }

    /// <summary>按参数名读取的只读视图，不用于写回</summary>
    IReadOnlyDictionary<string, object?> ArgumentsByName { get; }

    /// <summary>调用返回值，可在 Proceed 之后改写</summary>
    object? ReturnValue { get; set; }

    /// <summary>异步推进到目标方法或下一个拦截器，并写入 <see cref="ReturnValue"/></summary>
    ValueTask ProceedAsync();

    /// <summary>同步推进到目标方法；目标为异步时抛出明确异常</summary>
    void Proceed();
}
