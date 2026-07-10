using FluentValidation;
using MediatR;

namespace Tw.Application.Pipeline;

/// <summary>
/// 将 MediatR 请求接入应用层固定顺序管线的 pipeline behavior
/// </summary>
/// <typeparam name="TRequest">MediatR 请求类型</typeparam>
/// <typeparam name="TResponse">MediatR 响应类型</typeparam>
/// <param name="behaviors">应用层 pipeline behavior 集合</param>
/// <param name="completedHooks">handler 成功执行后的完成钩子集合</param>
/// <param name="validators">当前请求类型的 FluentValidation 校验器集合</param>
public sealed class MediatRApplicationPipelineBehavior<TRequest, TResponse>(
    IEnumerable<IApplicationPipelineBehavior> behaviors,
    IEnumerable<ICompletedHook> completedHooks,
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        TResponse? response = default;
        var orderedBehaviors = ApplicationPipelineOrder.CreateOrderedBehaviors(
            CreateBehaviors(request, behaviors, validators));
        var executor = new ApplicationPipelineExecutor(
            orderedBehaviors,
            completedHooks.ToArray());

        await executor.ExecuteAsync(
            async () => response = await next(),
            cancellationToken);

        return response!;
    }

    /// <summary>
    /// 创建Behaviors测试对象
    /// </summary>
    /// <param name="request">用于提供请求</param>
    /// <param name="behaviors">用于提供behaviors</param>
    /// <param name="validators">用于提供validators</param>
    /// <returns>匹配当前查询条件的结果集合</returns>
    private static IReadOnlyList<IApplicationPipelineBehavior> CreateBehaviors(
        TRequest request,
        IEnumerable<IApplicationPipelineBehavior> behaviors,
        IEnumerable<IValidator<TRequest>> validators)
    {
        var validatorList = validators.ToArray();
        if (validatorList.Length == 0)
        {
            return behaviors.ToArray();
        }

        return behaviors
            .Append(new FluentValidationApplicationPipelineBehavior<TRequest>(request, validatorList))
            .ToArray();
    }
}
