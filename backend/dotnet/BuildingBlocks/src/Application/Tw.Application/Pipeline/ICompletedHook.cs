namespace Tw.Application.Pipeline;

/// <summary>
/// 应用用例 handler 成功执行后的完成钩子
/// </summary>
public interface ICompletedHook
{
    /// <summary>
    /// 执行完成钩子
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步执行任务</returns>
    Task RunAsync(CancellationToken cancellationToken);
}
