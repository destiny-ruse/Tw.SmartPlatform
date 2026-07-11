using AwesomeAssertions;
using Xunit;

namespace Tw.Core.Tests.Async;

/// <summary>
/// 覆盖 <see cref="Tw.Async.AsyncDisposeFunc"/> 的异步释放契约
/// </summary>
public class AsyncDisposeFuncTests
{
    /// <summary>
    /// 验证多次释放只调用一次已配置委托
    /// </summary>
    [Fact]
    public async Task DisposeAsync_InvokesDelegateExactlyOnce()
    {
        var invocationCount = 0;
        var disposable = new Tw.Async.AsyncDisposeFunc(() =>
        {
            invocationCount++;
            return ValueTask.CompletedTask;
        });

        await disposable.DisposeAsync();
        await disposable.DisposeAsync();

        invocationCount.Should().Be(1);
    }

    /// <summary>
    /// 验证释放委托的异常会传递给调用方
    /// </summary>
    [Fact]
    public async Task DisposeAsync_PropagatesDelegateException()
    {
        var expectedException = new InvalidOperationException("异步释放失败");
        var disposable = new Tw.Async.AsyncDisposeFunc(() => ValueTask.FromException(expectedException));

        var act = async () => await disposable.DisposeAsync();

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
    }

    /// <summary>
    /// 验证释放会等待异步委托完成
    /// </summary>
    [Fact]
    public async Task DisposeAsync_AwaitsAsyncDelegate()
    {
        var completed = false;
        Func<Task> disposeAsync = async () =>
        {
            await Task.Yield();
            completed = true;
        };
        var disposable = new Tw.Async.AsyncDisposeFunc(disposeAsync);

        await disposable.DisposeAsync();

        completed.Should().BeTrue();
    }
}
