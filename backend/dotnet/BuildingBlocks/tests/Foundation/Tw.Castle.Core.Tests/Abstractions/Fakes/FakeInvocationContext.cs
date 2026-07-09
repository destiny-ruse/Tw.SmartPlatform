using System.Reflection;
using Tw.Castle.Core.Abstractions;

namespace Tw.Castle.Core.Tests.Abstractions.Fakes;

/// <summary>用于基类编排测试的最小 IInvocationContext 替身</summary>
internal sealed class FakeInvocationContext : IInvocationContext
{
    /// <summary>表示 _onProceed 字段</summary>
    private readonly Action? _onProceed;

    /// <summary>初始化 FakeInvocationContext 实例</summary>
    /// <param name="onProceed">onProceed 参数</param>
    public FakeInvocationContext(Action? onProceed = null)
    {
        _onProceed = onProceed;
    }

    /// <summary>表示 ProceedCount 属性</summary>
    public int ProceedCount { get; private set; }

    /// <summary>表示 Method 属性</summary>
    public MethodInfo Method => typeof(FakeInvocationContext).GetMethod(nameof(Sample))!;
    /// <summary>表示 Target 属性</summary>
    public object? Target => null;
    /// <summary>表示 Arguments 属性</summary>
    public object?[] Arguments { get; } = [];
    /// <summary>表示 ArgumentsByName 属性</summary>
    public IReadOnlyDictionary<string, object?> ArgumentsByName { get; } =
        new Dictionary<string, object?>();
    /// <summary>表示 ReturnValue 属性</summary>
    public object? ReturnValue { get; set; }

    /// <summary>验证 Proceed 场景</summary>
    public void Proceed()
    {
        ProceedCount++;
        _onProceed?.Invoke();
    }

    /// <summary>验证 ProceedAsync 场景</summary>
    /// <returns>ProceedAsync 的执行结果</returns>
    public ValueTask ProceedAsync()
    {
        Proceed();
        return ValueTask.CompletedTask;
    }

    /// <summary>验证 Sample 场景</summary>
    public void Sample()
    {
    }
}
