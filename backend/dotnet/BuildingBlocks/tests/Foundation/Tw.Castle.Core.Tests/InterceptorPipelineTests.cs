using System.Reflection;
using AwesomeAssertions;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests;

/// <summary>验证 InterceptorPipelineTests 相关行为</summary>
public class InterceptorPipelineTests
{
    /// <summary>验证 InterceptionReport_ExposesDiagnostics 场景</summary>
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

    /// <summary>验证 InvokeAsync_ExecutesInterceptorsInOrderAndProceedsOnce 场景</summary>
    /// <returns>InvokeAsync_ExecutesInterceptorsInOrderAndProceedsOnce 的执行结果</returns>
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

    /// <summary>验证 InvokeAsync_WithEmptyInterceptorChain_ProceedsTargetOnce 场景</summary>
    /// <returns>InvokeAsync_WithEmptyInterceptorChain_ProceedsTargetOnce 的执行结果</returns>
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

    /// <summary>验证 InvokeAsync_AllowsInterceptorToShortCircuitWithoutProceedingTarget 场景</summary>
    /// <returns>InvokeAsync_AllowsInterceptorToShortCircuitWithoutProceedingTarget 的执行结果</returns>
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

    /// <summary>验证 InvokeAsync_ThrowsAndKeepsTargetOnce_WhenInterceptorCallsProceedAsyncTwice 场景</summary>
    /// <returns>InvokeAsync_ThrowsAndKeepsTargetOnce_WhenInterceptorCallsProceedAsyncTwice 的执行结果</returns>
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

    /// <summary>验证 InvokeAsync_ThrowsAndKeepsTargetUncalled_WhenOuterInterceptorCallsProceedAsyncTwiceAfterInnerShortCircuit 场景</summary>
    /// <returns>InvokeAsync_ThrowsAndKeepsTargetUncalled_WhenOuterInterceptorCallsProceedAsyncTwiceAfterInnerShortCircuit 的执行结果</returns>
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

    /// <summary>验证 InvokeAsync_PropagatesTargetException 场景</summary>
    /// <returns>InvokeAsync_PropagatesTargetException 的执行结果</returns>
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

    /// <summary>验证 RecordingInterceptor 相关行为</summary>
    private sealed class RecordingInterceptor : IInterceptor
    {
        /// <summary>表示 _name 字段</summary>
        private readonly string _name;
        /// <summary>表示 _events 字段</summary>
        private readonly List<string> _events;

        /// <summary>初始化 RecordingInterceptor 实例</summary>
        /// <param name="name">name 参数</param>
        /// <param name="events">events 参数</param>
        public RecordingInterceptor(string name, List<string> events)
        {
            _name = name;
            _events = events;
        }

        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            _events.Add($"{_name}:before");
            await context.ProceedAsync();
            _events.Add($"{_name}:after");
        }
    }

    /// <summary>验证 ShortCircuitInterceptor 相关行为</summary>
    private sealed class ShortCircuitInterceptor : IInterceptor
    {
        /// <summary>表示 _events 字段</summary>
        private readonly List<string> _events;

        /// <summary>初始化 ShortCircuitInterceptor 实例</summary>
        /// <param name="events">events 参数</param>
        public ShortCircuitInterceptor(List<string> events)
        {
            _events = events;
        }

        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context)
        {
            _events.Add("short-circuit");
            context.ReturnValue = "short-circuited";

            return ValueTask.CompletedTask;
        }
    }

    /// <summary>验证 DoubleProceedInterceptor 相关行为</summary>
    private sealed class DoubleProceedInterceptor : IInterceptor
    {
        /// <summary>表示 _events 字段</summary>
        private readonly List<string> _events;

        /// <summary>初始化 DoubleProceedInterceptor 实例</summary>
        /// <param name="events">events 参数</param>
        public DoubleProceedInterceptor(List<string> events)
        {
            _events = events;
        }

        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            _events.Add("first-proceed");
            await context.ProceedAsync();
            _events.Add("second-proceed");
            await context.ProceedAsync();
        }
    }

    /// <summary>验证 RecordingInvocationContext 相关行为</summary>
    private sealed class RecordingInvocationContext : IInvocationContext
    {
        /// <summary>表示 _events 字段</summary>
        private readonly List<string> _events;
        /// <summary>表示 _proceedException 字段</summary>
        private readonly Exception? _proceedException;

        /// <summary>初始化 RecordingInvocationContext 实例</summary>
        /// <param name="events">events 参数</param>
        /// <param name="proceedException">proceedException 参数</param>
        public RecordingInvocationContext(List<string> events, Exception? proceedException = null)
        {
            _events = events;
            _proceedException = proceedException;
        }

        /// <summary>表示 Method 属性</summary>
        public MethodInfo Method { get; } = typeof(RecordingInvocationContext)
            .GetMethod(nameof(TargetMethod), BindingFlags.NonPublic | BindingFlags.Static)!;

        /// <summary>表示 Target 属性</summary>
        public object? Target => null;

        /// <summary>表示 Arguments 属性</summary>
        public object?[] Arguments { get; } = [];

        /// <summary>表示 ArgumentsByName 属性</summary>
        public IReadOnlyDictionary<string, object?> ArgumentsByName { get; } =
            new Dictionary<string, object?>(StringComparer.Ordinal);

        /// <summary>表示 ReturnValue 属性</summary>
        public object? ReturnValue { get; set; }

        /// <summary>表示 ProceedAsyncCallCount 属性</summary>
        public int ProceedAsyncCallCount { get; private set; }

        /// <summary>验证 ProceedAsync 场景</summary>
        /// <returns>ProceedAsync 的执行结果</returns>
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

        /// <summary>验证 Proceed 场景</summary>
        public void Proceed() => throw new NotSupportedException("测试上下文仅支持异步 Proceed");

        /// <summary>验证 TargetMethod 场景</summary>
        private static void TargetMethod()
        {
        }
    }
}
