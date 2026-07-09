namespace Tw.EventBus.Cap.Cleanup;

/// <summary>表示 CapMessageCleanupJob 类型</summary>
public sealed class CapMessageCleanupJob : ICapMessageCleanupJob
{
    /// <summary>执行 ExecuteAsync 操作</summary>
    /// <param name="options">options 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ExecuteAsync 的执行结果</returns>
    public Task ExecuteAsync(CapMessageCleanupOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
