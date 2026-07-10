namespace Tw.BackgroundJobs.Abstractions;

/// <summary>
/// 定义后台作业的能力边界
/// </summary>
/// <typeparam name="TArgs">响应数据的运行时类型</typeparam>
public interface IBackgroundJob<TArgs>
{
    /// <summary>
    /// 异步执行当前组件的核心处理流程
    /// </summary>
    /// <param name="args">用于提供args</param>
    /// <param name="context">当前调用携带的上下文信息</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task ExecuteAsync(TArgs args, BackgroundJobContext context, CancellationToken cancellationToken = default);
}
