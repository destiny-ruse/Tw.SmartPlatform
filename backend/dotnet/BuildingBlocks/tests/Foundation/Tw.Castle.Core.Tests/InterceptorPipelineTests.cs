using System.Reflection;
using AwesomeAssertions;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests;

/// <summary>
/// 覆盖拦截器管道的核心行为和边界条件
/// </summary>
public class InterceptorPipelineTests
{
    /// <summary>
    /// 验证nterception报告Exposes诊断集合
    /// </summary>
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

    /// <summary>
    /// 验证nvoke异步Executes拦截器集合InOrder和Proceeds一次
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证nvoke异步带有空拦截器ChainProceeds目标一次
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证nvoke异步Allows拦截器到短路Circuit不带Proceeding目标
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证nvoke异步抛出异常和Keeps目标一次当拦截器Calls继续处理异步两次
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证nvoke异步抛出异常和Keeps目标Uncalled当Outer拦截器Calls继续处理异步两次AfterInner短路Circuit
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证nvoke异步Propagates目标异常
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 覆盖Recording拦截器的核心行为和边界条件
    /// </summary>
    private sealed class RecordingInterceptor : IInterceptor
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的名称
        /// </summary>
        private readonly string _name;
        /// <summary>
        /// 保存当前类型处理流程依赖的events
        /// </summary>
        private readonly List<string> _events;

        /// <summary>
        /// 初始化 RecordingInterceptor 实例
        /// </summary>
        /// <param name="name">待匹配成员或资源的名称</param>
        /// <param name="events">用于提供events</param>
        public RecordingInterceptor(string name, List<string> events)
        {
            _name = name;
            _events = events;
        }

        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            _events.Add($"{_name}:before");
            await context.ProceedAsync();
            _events.Add($"{_name}:after");
        }
    }

    /// <summary>
    /// 覆盖短路Circuit拦截器的核心行为和边界条件
    /// </summary>
    private sealed class ShortCircuitInterceptor : IInterceptor
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的events
        /// </summary>
        private readonly List<string> _events;

        /// <summary>
        /// 初始化 ShortCircuitInterceptor 实例
        /// </summary>
        /// <param name="events">用于提供events</param>
        public ShortCircuitInterceptor(List<string> events)
        {
            _events = events;
        }

        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context)
        {
            _events.Add("short-circuit");
            context.ReturnValue = "short-circuited";

            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// 覆盖Double继续处理拦截器的核心行为和边界条件
    /// </summary>
    private sealed class DoubleProceedInterceptor : IInterceptor
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的events
        /// </summary>
        private readonly List<string> _events;

        /// <summary>
        /// 初始化 DoubleProceedInterceptor 实例
        /// </summary>
        /// <param name="events">用于提供events</param>
        public DoubleProceedInterceptor(List<string> events)
        {
            _events = events;
        }

        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            _events.Add("first-proceed");
            await context.ProceedAsync();
            _events.Add("second-proceed");
            await context.ProceedAsync();
        }
    }

    /// <summary>
    /// 覆盖Recording调用上下文的核心行为和边界条件
    /// </summary>
    private sealed class RecordingInvocationContext : IInvocationContext
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的events
        /// </summary>
        private readonly List<string> _events;
        /// <summary>
        /// 保存当前类型处理流程依赖的继续处理Exception
        /// </summary>
        private readonly Exception? _proceedException;

        /// <summary>
        /// 初始化 RecordingInvocationContext 实例
        /// </summary>
        /// <param name="events">用于提供events</param>
        /// <param name="proceedException">用于提供proceed异常</param>
        public RecordingInvocationContext(List<string> events, Exception? proceedException = null)
        {
            _events = events;
            _proceedException = proceedException;
        }

        /// <summary>
        /// typeof在当前对象中的业务含义
        /// </summary>
        public MethodInfo Method { get; } = typeof(RecordingInvocationContext)
            .GetMethod(nameof(TargetMethod), BindingFlags.NonPublic | BindingFlags.Static)!;

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
            new Dictionary<string, object?>(StringComparer.Ordinal);

        /// <summary>
        /// 拦截流程返回给调用方的结果对象
        /// </summary>
        public object? ReturnValue { get; set; }

        /// <summary>
        /// 继续处理异步Call数量在当前对象中的业务含义
        /// </summary>
        public int ProceedAsyncCallCount { get; private set; }

        /// <summary>
        /// 说明ProceedAsync在当前类型中的职责
        /// </summary>
        /// <returns>表示异步流程完成状态的任务</returns>
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

        /// <summary>
        /// 说明Proceed在当前类型中的职责
        /// </summary>
        public void Proceed() => throw new NotSupportedException("测试上下文仅支持异步 Proceed");

        /// <summary>
        /// 说明Target方法在当前类型中的职责
        /// </summary>
        private static void TargetMethod()
        {
        }
    }
}
