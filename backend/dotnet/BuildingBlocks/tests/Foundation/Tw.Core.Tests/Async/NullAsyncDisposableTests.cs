using AwesomeAssertions;
using Xunit;

namespace Tw.Core.Tests.Async;

/// <summary>
/// 覆盖 <see cref="Tw.Async.NullAsyncDisposable"/> 的空操作释放契约
/// </summary>
public class NullAsyncDisposableTests
{
    /// <summary>
    /// 验证共享实例可以安全地重复异步释放
    /// </summary>
    [Fact]
    public async Task DisposeAsync_CanBeReusedSafely()
    {
        var first = Tw.Async.NullAsyncDisposable.Instance.DisposeAsync();
        var second = Tw.Async.NullAsyncDisposable.Instance.DisposeAsync();

        first.IsCompletedSuccessfully.Should().BeTrue();
        second.IsCompletedSuccessfully.Should().BeTrue();
        await first;
        await second;
    }
}
