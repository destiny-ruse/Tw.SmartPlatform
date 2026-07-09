using AwesomeAssertions;
using Tw.Application.Pipeline;
using Xunit;

namespace Tw.Application.Tests.Pipeline;

/// <summary>验证 ApplicationPipelineExecutorTests 相关行为</summary>
public sealed class ApplicationPipelineExecutorTests
{
    /// <summary>验证 ExecuteAsync_RunsBehaviorsInSpecOrder 场景</summary>
    /// <returns>ExecuteAsync_RunsBehaviorsInSpecOrder 的执行结果</returns>
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

    /// <summary>验证 ExecuteAsync_RunsCompletedHooksAfterHandler 场景</summary>
    /// <returns>ExecuteAsync_RunsCompletedHooksAfterHandler 的执行结果</returns>
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

    /// <summary>验证 RecordingBehavior 相关行为</summary>
    private sealed class RecordingBehavior(string name, List<string> calls) : IApplicationPipelineBehavior
    {
        /// <summary>表示 Name 属性</summary>
        public string Name => name;

        /// <summary>验证 InvokeAsync 场景</summary>
        /// <param name="next">next 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>InvokeAsync 的执行结果</returns>
        public async Task InvokeAsync(Func<Task> next, CancellationToken cancellationToken)
        {
            calls.Add($"{name}-before");
            await next();
            calls.Add($"{name}-after");
        }
    }

    /// <summary>验证 RecordingCompletedHook 相关行为</summary>
    private sealed class RecordingCompletedHook(List<string> calls) : ICompletedHook
    {
        /// <summary>验证 RunAsync 场景</summary>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>RunAsync 的执行结果</returns>
        public Task RunAsync(CancellationToken cancellationToken)
        {
            calls.Add("CompletedHook");
            return Task.CompletedTask;
        }
    }
}
