namespace Tw.Application.Pipeline;

/// <summary>
/// 应用用例 pipeline behavior 的固定排序规则
/// </summary>
public static class ApplicationPipelineOrder
{
    /// <summary>
    /// 保存当前类型处理流程依赖的Order
    /// </summary>
    private static readonly string[] Order =
    [
        "ExecutionContext",
        "Feature",
        "Authorization",
        "Validation",
        "Idempotency",
        "Sharding",
        "Uow",
        "Concurrency",
        "Auditing"
    ];

    /// <summary>
    /// 根据固定顺序排列 pipeline behavior，未知名称排在已知行为之后
    /// </summary>
    /// <param name="behaviors">待排序的 pipeline behavior 集合</param>
    /// <returns>排序后的 pipeline behavior 列表</returns>
    /// <exception cref="ArgumentNullException"><paramref name="behaviors"/> 为 <see langword="null"/> 时抛出</exception>
    public static IReadOnlyList<IApplicationPipelineBehavior> CreateOrderedBehaviors(
        IEnumerable<IApplicationPipelineBehavior> behaviors)
    {
        ArgumentNullException.ThrowIfNull(behaviors);

        return behaviors
            .OrderBy(behavior =>
            {
                var index = Array.IndexOf(Order, behavior.Name);
                return index < 0 ? int.MaxValue : index;
            })
            .ToArray();
    }
}
