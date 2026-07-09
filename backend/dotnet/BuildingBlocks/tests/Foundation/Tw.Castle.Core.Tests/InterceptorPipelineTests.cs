using System.Reflection;
using AwesomeAssertions;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests;

public class InterceptorPipelineTests
{
    [Fact]
    public void InterceptionReport_ExposesDiagnostics()
    {
        var item = new InterceptionDiagnostic(
            ServiceTypeName: "Sample.IOrderService",
            ImplementationTypeName: "Sample.OrderService",
            MethodName: "SubmitAsync",
            Carrier: "CastleInterfaceProxy",
            InterceptorTypeNames: ["Sample.AuditInterceptor"],
            Status: "enabled",
            Reason: null);

        var report = new InterceptionReport([item]);

        report.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
    }

    [Fact]
    public async Task InvokeAsync_ExecutesInterceptorsInOrderAndProceedsOnce()
    {
        var events = new List<string>();
        var context = new RecordingInvocationContext(events);
        var pipeline = new InterceptorPipeline();
        var interceptors = new IInterceptor[]
        {
            new RecordingInterceptor("first", events),
            new RecordingInterceptor("second", events),
        };

        await pipeline.InvokeAsync(context, interceptors);

        events.Should().Equal("first:before", "second:before", "target", "second:after", "first:after");
        context.ProceedAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyInterceptorChain_ProceedsTargetOnce()
    {
        var events = new List<string>();
        var context = new RecordingInvocationContext(events);
        var pipeline = new InterceptorPipeline();

        await pipeline.InvokeAsync(context, []);

        events.Should().Equal("target");
        context.ProceedAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_AllowsInterceptorToShortCircuitWithoutProceedingTarget()
    {
        var events = new List<string>();
        var context = new RecordingInvocationContext(events);
        var pipeline = new InterceptorPipeline();

        await pipeline.InvokeAsync(context, [new ShortCircuitInterceptor(events)]);

        events.Should().Equal("short-circuit");
        context.ProceedAsyncCallCount.Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_ThrowsAndKeepsTargetOnce_WhenInterceptorCallsProceedAsyncTwice()
    {
        var events = new List<string>();
        var context = new RecordingInvocationContext(events);
        var pipeline = new InterceptorPipeline();

        var act = async () => await pipeline.InvokeAsync(context, [new DoubleProceedInterceptor(events)]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Proceed*");
        context.ProceedAsyncCallCount.Should().Be(1);
        events.Should().Equal("first-proceed", "target", "second-proceed");
    }

    [Fact]
    public async Task InvokeAsync_ThrowsAndKeepsTargetUncalled_WhenOuterInterceptorCallsProceedAsyncTwiceAfterInnerShortCircuit()
    {
        var events = new List<string>();
        var context = new RecordingInvocationContext(events);
        var pipeline = new InterceptorPipeline();

        var act = async () => await pipeline.InvokeAsync(context, [
            new DoubleProceedInterceptor(events),
            new ShortCircuitInterceptor(events),
        ]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Proceed*");
        context.ProceedAsyncCallCount.Should().Be(0);
        events.Should().Equal("first-proceed", "short-circuit", "second-proceed");
    }

    [Fact]
    public async Task InvokeAsync_PropagatesTargetException()
    {
        var events = new List<string>();
        var targetException = new InvalidOperationException("目标失败");
        var context = new RecordingInvocationContext(events, targetException);
        var pipeline = new InterceptorPipeline();

        var act = async () => await pipeline.InvokeAsync(context, [new RecordingInterceptor("first", events)]);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(targetException);
        context.ProceedAsyncCallCount.Should().Be(1);
        events.Should().Equal("first:before", "target");
    }

    private sealed class RecordingInterceptor : IInterceptor
    {
        private readonly string _name;
        private readonly List<string> _events;

        public RecordingInterceptor(string name, List<string> events)
        {
            _name = name;
            _events = events;
        }

        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            _events.Add($"{_name}:before");
            await context.ProceedAsync();
            _events.Add($"{_name}:after");
        }
    }

    private sealed class ShortCircuitInterceptor : IInterceptor
    {
        private readonly List<string> _events;

        public ShortCircuitInterceptor(List<string> events)
        {
            _events = events;
        }

        public ValueTask InterceptAsync(IInvocationContext context)
        {
            _events.Add("short-circuit");
            context.ReturnValue = "short-circuited";

            return ValueTask.CompletedTask;
        }
    }

    private sealed class DoubleProceedInterceptor : IInterceptor
    {
        private readonly List<string> _events;

        public DoubleProceedInterceptor(List<string> events)
        {
            _events = events;
        }

        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            _events.Add("first-proceed");
            await context.ProceedAsync();
            _events.Add("second-proceed");
            await context.ProceedAsync();
        }
    }

    private sealed class RecordingInvocationContext : IInvocationContext
    {
        private readonly List<string> _events;
        private readonly Exception? _proceedException;

        public RecordingInvocationContext(List<string> events, Exception? proceedException = null)
        {
            _events = events;
            _proceedException = proceedException;
        }

        public MethodInfo Method { get; } = typeof(RecordingInvocationContext)
            .GetMethod(nameof(TargetMethod), BindingFlags.NonPublic | BindingFlags.Static)!;

        public object? Target => null;

        public object?[] Arguments { get; } = [];

        public IReadOnlyDictionary<string, object?> ArgumentsByName { get; } =
            new Dictionary<string, object?>(StringComparer.Ordinal);

        public object? ReturnValue { get; set; }

        public int ProceedAsyncCallCount { get; private set; }

        public ValueTask ProceedAsync()
        {
            ProceedAsyncCallCount++;
            _events.Add("target");
            if (_proceedException is not null)
            {
                throw _proceedException;
            }

            ReturnValue = "completed";

            return ValueTask.CompletedTask;
        }

        public void Proceed() => throw new NotSupportedException("测试上下文仅支持异步 Proceed");

        private static void TargetMethod()
        {
        }
    }
}
