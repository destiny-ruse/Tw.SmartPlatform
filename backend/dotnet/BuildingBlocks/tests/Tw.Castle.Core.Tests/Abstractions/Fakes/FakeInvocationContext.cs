using System.Reflection;
using Tw.Castle.Core.Abstractions;

namespace Tw.Castle.Core.Tests.Abstractions.Fakes;

/// <summary>用于基类编排测试的最小 IInvocationContext 替身</summary>
internal sealed class FakeInvocationContext : IInvocationContext
{
    private readonly Action? _onProceed;

    public FakeInvocationContext(Action? onProceed = null)
    {
        _onProceed = onProceed;
    }

    public int ProceedCount { get; private set; }

    public MethodInfo Method => typeof(FakeInvocationContext).GetMethod(nameof(Sample))!;
    public object? Target => null;
    public object?[] Arguments { get; } = [];
    public IReadOnlyDictionary<string, object?> ArgumentsByName { get; } =
        new Dictionary<string, object?>();
    public object? ReturnValue { get; set; }

    public void Proceed()
    {
        ProceedCount++;
        _onProceed?.Invoke();
    }

    public ValueTask ProceedAsync()
    {
        Proceed();
        return ValueTask.CompletedTask;
    }

    public void Sample()
    {
    }
}
