using FluentValidation;

namespace Tw.Application.Pipeline;

/// <summary>表示 FluentValidationApplicationPipelineBehavior 类型</summary>
/// <typeparam name="TRequest">TRequest 类型参数</typeparam>
internal sealed class FluentValidationApplicationPipelineBehavior<TRequest>(
    TRequest request,
    IReadOnlyList<IValidator<TRequest>> validators) : IApplicationPipelineBehavior
{
    /// <summary>表示 Name 属性</summary>
    public string Name => "Validation";

    /// <summary>执行 InvokeAsync 操作</summary>
    /// <param name="next">next 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>InvokeAsync 的执行结果</returns>
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
