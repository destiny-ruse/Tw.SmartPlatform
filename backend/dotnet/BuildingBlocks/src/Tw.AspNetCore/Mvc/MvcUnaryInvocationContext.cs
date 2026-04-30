using System.Reflection;
using Tw.DependencyInjection.Invocation;

namespace Tw.AspNetCore.Mvc;

internal sealed class MvcUnaryInvocationContext : IUnaryInvocationContext
{
    private Func<ValueTask>? _proceed;

    public MvcUnaryInvocationContext(
        MethodInfo method,
        object?[] arguments,
        Type returnType,
        CancellationToken cancellationToken)
    {
        Method = method;
        Arguments = arguments;
        ReturnType = returnType;
        CancellationToken = cancellationToken;
        Items = new Dictionary<string, object?>(StringComparer.Ordinal);
        Features = new InvocationFeatureCollection();
    }

    public CancellationToken CancellationToken { get; }

    public IDictionary<string, object?> Items { get; }

    public MethodInfo Method { get; }

    public object?[] Arguments { get; }

    public Type ReturnType { get; }

    public object? ReturnValue { get; set; }

    public InvocationFeatureCollection Features { get; }

    public TFeature? GetFeature<TFeature>() where TFeature : class => Features.Get<TFeature>();

    public ValueTask ProceedAsync()
        => _proceed?.Invoke()
           ?? throw new InvalidOperationException("ProceedAsync 尚未初始化");

    public void SetProceed(Func<ValueTask> proceed)
        => _proceed = proceed;
}
