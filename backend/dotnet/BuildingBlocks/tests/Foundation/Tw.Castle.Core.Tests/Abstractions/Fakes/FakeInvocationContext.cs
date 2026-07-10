using System.Reflection;
using Tw.Castle.Core.Abstractions;

namespace Tw.Castle.Core.Tests.Abstractions.Fakes;

/// <summary>
/// 用于基类编排测试的最小 IInvocationContext 替身
/// </summary>
internal sealed class FakeInvocationContext : IInvocationContext
{
    /// <summary>
    /// 保存当前类型处理流程依赖的on继续处理
    /// </summary>
    private readonly Action? _onProceed;

    /// <summary>
    /// 初始化 FakeInvocationContext 实例
    /// </summary>
    /// <param name="onProceed">用于提供onProceed</param>
    public FakeInvocationContext(Action? onProceed = null)
    {
        _onProceed = onProceed;
    }

    /// <summary>
    /// 继续处理数量在当前对象中的业务含义
    /// </summary>
    public int ProceedCount { get; private set; }

    /// <summary>
    /// typeof在当前对象中的业务含义
    /// </summary>
    public MethodInfo Method => typeof(FakeInvocationContext).GetMethod(nameof(Sample))!;
    /// <summary>
    /// 目标在当前对象中的业务含义
    /// </summary>
    public object? Target => null;
    /// <summary>
    /// 参数在当前对象中的业务含义
    /// </summary>
    public object?[] Arguments { get; } = [];
    /// <summary>
    /// 当前调用按名称索引后的参数集合
    /// </summary>
    public IReadOnlyDictionary<string, object?> ArgumentsByName { get; } =
        new Dictionary<string, object?>();
    /// <summary>
    /// 拦截流程返回给调用方的结果对象
    /// </summary>
    public object? ReturnValue { get; set; }

    /// <summary>
    /// 说明Proceed在当前类型中的职责
    /// </summary>
    public void Proceed()
    {
        ProceedCount++;
        _onProceed?.Invoke();
    }

    /// <summary>
    /// 说明ProceedAsync在当前类型中的职责
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    public ValueTask ProceedAsync()
    {
        Proceed();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 说明Sample在当前类型中的职责
    /// </summary>
    public void Sample()
    {
    }
}
