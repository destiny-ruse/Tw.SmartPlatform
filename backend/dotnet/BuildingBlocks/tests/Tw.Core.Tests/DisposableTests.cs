using FluentAssertions;
using Tw.Core.Utilities;
using Xunit;

namespace Tw.Core.Tests;

public class DisposableTests
{
    [Fact]
    public void DisposeAction_Invokes_Action_Once()
    {
        var callCount = 0;
        var disposable = new DisposeAction(() => callCount++);

        disposable.Dispose();
        disposable.Dispose();

        callCount.Should().Be(1);
    }

    [Fact]
    public void DisposeAction_Rejects_Null_Action()
    {
        Action action = null!;

        var act = () => new DisposeAction(action);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(action));
    }

    [Fact]
    public void DisposeAction_Invokes_Action_Once_For_Concurrent_Dispose()
    {
        var callCount = 0;
        var disposable = new DisposeAction(() => Interlocked.Increment(ref callCount));

        Parallel.For(0, 16, _ => disposable.Dispose());

        callCount.Should().Be(1);
    }

    [Fact]
    public async Task AsyncDisposeFunc_Invokes_Function_Once()
    {
        var callCount = 0;
        var disposable = new AsyncDisposeFunc(() =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        await disposable.DisposeAsync();
        await disposable.DisposeAsync();

        callCount.Should().Be(1);
    }

    [Fact]
    public void AsyncDisposeFunc_Rejects_Null_Function()
    {
        Func<Task> disposeAsync = null!;

        var act = () => new AsyncDisposeFunc(disposeAsync);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(disposeAsync));
    }

    [Fact]
    public async Task AsyncDisposeFunc_Invokes_Function_Once_For_Concurrent_Dispose()
    {
        var callCount = 0;
        var disposable = new AsyncDisposeFunc(() =>
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        });

        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => disposable.DisposeAsync().AsTask()));

        callCount.Should().Be(1);
    }

    [Fact]
    public async Task AsyncDisposeFunc_Propagates_Synchronous_Exception_From_Task_Function()
    {
        var expected = new InvalidOperationException("同步失败");
        Func<Task> disposeAsync = () => throw expected;
        var disposable = new AsyncDisposeFunc(disposeAsync);

        var act = async () => await disposable.DisposeAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(expected.Message);
    }

    [Fact]
    public async Task AsyncDisposeFunc_Propagates_Faulted_Task()
    {
        var expected = new InvalidOperationException("任务失败");
        var disposable = new AsyncDisposeFunc(() => Task.FromException(expected));

        var act = async () => await disposable.DisposeAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(expected.Message);
    }

    [Fact]
    public async Task AsyncDisposeFunc_Propagates_Faulted_ValueTask()
    {
        var expected = new InvalidOperationException("value 任务失败");
        var disposable = new AsyncDisposeFunc(() => ValueTask.FromException(expected));

        var act = async () => await disposable.DisposeAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(expected.Message);
    }

    [Fact]
    public async Task Null_Disposables_Are_Safe()
    {
        NullDisposable.Instance.Dispose();
        await NullAsyncDisposable.Instance.DisposeAsync();
    }
}
