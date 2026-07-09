namespace Tw.Application.Pipeline;

/// <summary>
/// 应用用例执行管线中的环绕行为
/// </summary>
public interface IApplicationPipelineBehavior
{
    /// <summary>
    /// 行为名称，用于确定固定执行顺序
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 执行当前行为并决定是否继续调用后续行为或 handler
    /// </summary>
    /// <param name="next">后续行为或 handler</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步执行任务</returns>
    Task InvokeAsync(Func<Task> next, CancellationToken cancellationToken);
}
