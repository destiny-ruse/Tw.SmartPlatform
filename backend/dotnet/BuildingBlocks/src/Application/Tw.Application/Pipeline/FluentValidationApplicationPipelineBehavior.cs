using FluentValidation;

namespace Tw.Application.Pipeline;

internal sealed class FluentValidationApplicationPipelineBehavior<TRequest>(
    TRequest request,
    IReadOnlyList<IValidator<TRequest>> validators) : IApplicationPipelineBehavior
{
    public string Name => "Validation";

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
