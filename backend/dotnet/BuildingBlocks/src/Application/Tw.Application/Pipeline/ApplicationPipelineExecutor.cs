namespace Tw.Application.Pipeline;

/// <summary>
/// 按固定顺序执行应用用例 pipeline behavior 与完成钩子
/// </summary>
/// <param name="behaviors">已排序的 pipeline behavior 列表</param>
/// <param name="completedHooks">handler 成功执行后的完成钩子列表</param>
public sealed class ApplicationPipelineExecutor(
    IReadOnlyList<IApplicationPipelineBehavior> behaviors,
    IReadOnlyList<ICompletedHook>? completedHooks = null)
{
    /// <summary>
    /// 执行 handler 及其外层 pipeline behavior，handler 成功后依次执行 completed hook
    /// </summary>
    /// <param name="handler">应用用例 handler</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步执行任务</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> 为 <see langword="null"/> 时抛出</exception>
    public async Task ExecuteAsync(Func<Task> handler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        Func<Task> next = handler;
        for (var index = behaviors.Count - 1; index >= 0; index--)
        {
            var behavior = behaviors[index];
            var current = next;
            next = () => behavior.InvokeAsync(current, cancellationToken);
        }

        await next();

        foreach (var completedHook in completedHooks ?? Array.Empty<ICompletedHook>())
        {
            await completedHook.RunAsync(cancellationToken);
        }
    }
}
