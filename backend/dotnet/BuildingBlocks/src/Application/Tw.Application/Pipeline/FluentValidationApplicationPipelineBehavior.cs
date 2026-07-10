using FluentValidation;

namespace Tw.Application.Pipeline;

/// <summary>
/// 封装FluentValidationApplication管道行为相关的数据和行为
/// </summary>
/// <typeparam name="TRequest">响应数据的运行时类型</typeparam>
internal sealed class FluentValidationApplicationPipelineBehavior<TRequest>(
    TRequest request,
    IReadOnlyList<IValidator<TRequest>> validators) : IApplicationPipelineBehavior
{
    /// <summary>
    /// 名称在当前对象中的业务含义
    /// </summary>
    public string Name => "Validation";

    /// <summary>
    /// 执行测试管道委托并记录调用
    /// </summary>
    /// <param name="next">用于提供next</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    public async Task InvokeAsync(Func<Task> next, CancellationToken cancellationToken)
    {
        if (validators.Count > 0)
        {
            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(
                validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));
            var failures = results
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }
        }

        await next();
    }
}
