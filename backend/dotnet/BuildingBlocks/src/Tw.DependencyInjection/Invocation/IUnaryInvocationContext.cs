using System.Reflection;

namespace Tw.DependencyInjection.Invocation;

/// <summary>
/// 单次方法调用上下文，在 <see cref="IInvocationContext"/> 基础上追加方法元数据和返回值控制。
/// </summary>
public interface IUnaryInvocationContext : IInvocationContext
{
    /// <summary>
    /// 被拦截方法的反射元数据。
    /// </summary>
    MethodInfo Method { get; }

    /// <summary>
    /// 调用时传入的参数列表，顺序与方法签名一致。
    /// </summary>
    object?[] Arguments { get; }

    /// <summary>
    /// 方法的返回类型（非 Task/ValueTask 包装层的实际类型）。
    /// </summary>
    Type ReturnType { get; }

    /// <summary>
    /// 方法的返回值；拦截器可在 <see cref="ProceedAsync"/> 之后读写。
    /// </summary>
    object? ReturnValue { get; set; }

    /// <summary>
    /// 推进到下一个拦截器或目标方法。
    /// </summary>
    ValueTask ProceedAsync();
}
