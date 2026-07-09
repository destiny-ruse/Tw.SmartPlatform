namespace Tw.EventBus.Cap.Cleanup;

/// <summary>定义 ICapMessageCleanupJob 契约</summary>
public interface ICapMessageCleanupJob
{
    /// <summary>执行 ExecuteAsync 操作</summary>
    /// <param name="options">options 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ExecuteAsync 的执行结果</returns>
    Task ExecuteAsync(CapMessageCleanupOptions options, CancellationToken cancellationToken = default);
}
