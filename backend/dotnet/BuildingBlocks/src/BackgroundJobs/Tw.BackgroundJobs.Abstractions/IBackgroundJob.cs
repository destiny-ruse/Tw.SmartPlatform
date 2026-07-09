namespace Tw.BackgroundJobs.Abstractions;

/// <summary>定义 IBackgroundJob 契约</summary>
/// <typeparam name="TArgs">TArgs 类型参数</typeparam>
public interface IBackgroundJob<TArgs>
{
    /// <summary>执行 ExecuteAsync 操作</summary>
    /// <param name="args">args 参数</param>
    /// <param name="context">context 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ExecuteAsync 的执行结果</returns>
    Task ExecuteAsync(TArgs args, BackgroundJobContext context, CancellationToken cancellationToken = default);
}
