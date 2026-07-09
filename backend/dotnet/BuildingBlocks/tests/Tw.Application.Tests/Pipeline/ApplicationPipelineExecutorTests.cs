using AwesomeAssertions;
using Tw.Application.Pipeline;
using Xunit;

namespace Tw.Application.Tests.Pipeline;

public sealed class ApplicationPipelineExecutorTests
{
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

    private sealed class RecordingBehavior(string name, List<string> calls) : IApplicationPipelineBehavior
    {
        public string Name => name;

        public async Task InvokeAsync(Func<Task> next, CancellationToken cancellationToken)
        {
            calls.Add($"{name}-before");
            await next();
            calls.Add($"{name}-after");
        }
    }

    private sealed class RecordingCompletedHook(List<string> calls) : ICompletedHook
    {
        public Task RunAsync(CancellationToken cancellationToken)
        {
            calls.Add("CompletedHook");
            return Task.CompletedTask;
        }
    }
}
