using AwesomeAssertions;
using Tw.Application.Pipeline;
using Xunit;

namespace Tw.Application.Tests.Pipeline;

/// <summary>
/// 覆盖Application管道Executor的核心行为和边界条件
/// </summary>
public sealed class ApplicationPipelineExecutorTests
{
    /// <summary>
    /// 验证执行异步RunsBehaviorsInSpecOrder
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task ExecuteAsync_RunsBehaviorsInSpecOrder()
    {
        var calls = new List<string>();
        var behaviors = ApplicationPipelineOrder.CreateOrderedBehaviors([
            new RecordingBehavior("Auditing", calls),
            new RecordingBehavior("Validation", calls),
            new RecordingBehavior("Authorization", calls)
        ]);
        var executor = new ApplicationPipelineExecutor(behaviors);

        await executor.ExecuteAsync(
            () =>
            {
                calls.Add("Handler");
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken);

        calls.Should().Equal(
            "Authorization-before",
            "Validation-before",
            "Auditing-before",
            "Handler",
            "Auditing-after",
            "Validation-after",
            "Authorization-after");
    }

    /// <summary>
    /// 验证执行异步RunsCompletedHooksAfter处理器
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task ExecuteAsync_RunsCompletedHooksAfterHandler()
    {
        var calls = new List<string>();
        var executor = new ApplicationPipelineExecutor(
            Array.Empty<IApplicationPipelineBehavior>(),
            [new RecordingCompletedHook(calls)]);

        await executor.ExecuteAsync(
            () =>
            {
                calls.Add("Handler");
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken);

        calls.Should().Equal("Handler", "CompletedHook");
    }

    /// <summary>
    /// 覆盖Recording行为的核心行为和边界条件
    /// </summary>
    private sealed class RecordingBehavior(string name, List<string> calls) : IApplicationPipelineBehavior
    {
        /// <summary>
        /// 名称在当前对象中的业务含义
        /// </summary>
        public string Name => name;

        /// <summary>
        /// 执行测试管道委托并记录调用
        /// </summary>
        /// <param name="next">用于提供next</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public async Task InvokeAsync(Func<Task> next, CancellationToken cancellationToken)
        {
            calls.Add($"{name}-before");
            await next();
            calls.Add($"{name}-after");
        }
    }

    /// <summary>
    /// 覆盖RecordingCompletedHook的核心行为和边界条件
    /// </summary>
    private sealed class RecordingCompletedHook(List<string> calls) : ICompletedHook
    {
        /// <summary>
        /// 运行测试管道委托
        /// </summary>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task RunAsync(CancellationToken cancellationToken)
        {
            calls.Add("CompletedHook");
            return Task.CompletedTask;
        }
    }
}
