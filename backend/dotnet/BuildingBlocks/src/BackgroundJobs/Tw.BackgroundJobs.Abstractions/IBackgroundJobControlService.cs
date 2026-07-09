namespace Tw.BackgroundJobs.Abstractions;

/// <summary>定义 IBackgroundJobControlService 契约</summary>
public interface IBackgroundJobControlService
{
    /// <summary>执行 ExecuteAsync 操作</summary>
    /// <param name="command">command 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ExecuteAsync 的执行结果</returns>
    Task ExecuteAsync(BackgroundJobControlCommand command, CancellationToken cancellationToken = default);
}
