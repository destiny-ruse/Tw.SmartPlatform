namespace Tw.Core.Utilities;

/// <summary>
/// 提供释放时无任何效果的可复用异步释放实例
/// </summary>
public sealed class NullAsyncDisposable : IAsyncDisposable
{
    /// <summary>
    /// 共享的空操作异步释放实例
    /// </summary>
    public static NullAsyncDisposable Instance { get; } = new();

    private NullAsyncDisposable()
    {
    }

    /// <summary>
    /// 不执行任何工作并直接完成
    /// </summary>
    /// <returns>已完成的值任务</returns>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
