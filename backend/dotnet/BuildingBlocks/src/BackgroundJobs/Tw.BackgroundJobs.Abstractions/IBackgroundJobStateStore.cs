namespace Tw.BackgroundJobs.Abstractions;

/// <summary>定义 IBackgroundJobStateStore 契约</summary>
public interface IBackgroundJobStateStore
{
    /// <summary>执行 SaveAsync 操作</summary>
    /// <param name="definition">definition 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>SaveAsync 的执行结果</returns>
    Task SaveAsync(BackgroundJobDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>执行 MarkPausedAsync 操作</summary>
    /// <param name="jobName">jobName 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>MarkPausedAsync 的执行结果</returns>
    Task MarkPausedAsync(string jobName, CancellationToken cancellationToken = default);

    /// <summary>执行 MarkRunningAsync 操作</summary>
    /// <param name="jobName">jobName 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>MarkRunningAsync 的执行结果</returns>
    Task MarkRunningAsync(string jobName, CancellationToken cancellationToken = default);

    /// <summary>执行 MarkStoppedAsync 操作</summary>
    /// <param name="jobName">jobName 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>MarkStoppedAsync 的执行结果</returns>
    Task MarkStoppedAsync(string jobName, CancellationToken cancellationToken = default);
}
