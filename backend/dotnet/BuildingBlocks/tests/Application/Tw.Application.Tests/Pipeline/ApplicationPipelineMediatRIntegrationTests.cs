using AwesomeAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tw.Application.Pipeline;
using Xunit;

namespace Tw.Application.Tests.Pipeline;

public sealed class ApplicationPipelineMediatRIntegrationTests
{
    [Fact]
    public async Task AddApplicationPipeline_ExecutesMediatRRequestsThroughOrderedApplicationPipeline()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(calls);
        services.AddSingleton<IApplicationPipelineBehavior>(new RecordingBehavior("Auditing", calls));
        services.AddSingleton<IApplicationPipelineBehavior>(new RecordingBehavior("Validation", calls));
        services.AddSingleton<IApplicationPipelineBehavior>(new RecordingBehavior("Authorization", calls));
        services.AddSingleton<ICompletedHook>(new RecordingCompletedHook(calls));

        services.AddApplicationPipeline(typeof(SampleRequestHandler).Assembly);

        await using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<IMediator>()
            .Send(new SampleRequest(), TestContext.Current.CancellationToken);

        result.Should().Be("handled");
        calls.Should().Equal(
            "Authorization-before",
            "Validation-before",
            "Auditing-before",
            "Handler",
            "Auditing-after",
            "Validation-after",
            "Authorization-after",
            "CompletedHook");
    }

    [Fact]
    public async Task AddApplicationPipeline_RunsFluentValidatorsAtValidationPipelinePosition()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(calls);
        services.AddSingleton<IApplicationPipelineBehavior>(new RecordingBehavior("Auditing", calls));
        services.AddSingleton<IApplicationPipelineBehavior>(new RecordingBehavior("Authorization", calls));
        services.AddSingleton<IValidator<SampleRequest>>(new RecordingValidator(calls));

        services.AddApplicationPipeline(typeof(SampleRequestHandler).Assembly);

        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IMediator>()
            .Send(new SampleRequest(), TestContext.Current.CancellationToken);

        calls.Should().Equal(
            "Authorization-before",
            "Validator",
            "Auditing-before",
            "Handler",
            "Auditing-after",
            "Authorization-after");
    }

    private sealed record SampleRequest : IRequest<string>;

    private sealed class SampleRequestHandler(List<string> calls) : IRequestHandler<SampleRequest, string>
    {
        public Task<string> Handle(SampleRequest request, CancellationToken cancellationToken)
        {
            calls.Add("Handler");
            return Task.FromResult("handled");
        }
    }

    private sealed class RecordingBehavior(string name, List<string> calls) : IApplicationPipelineBehavior
    {
        public string Name => name;

        public async Task InvokeAsync(Func<Task> next, CancellationToken cancellationToken)
        {
            calls.Add($"{name}-before");
            await next();
            calls.Add($"{name}-after");
        }
    }

    private sealed class RecordingCompletedHook(List<string> calls) : ICompletedHook
    {
        public Task RunAsync(CancellationToken cancellationToken)
        {
            calls.Add("CompletedHook");
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingValidator : AbstractValidator<SampleRequest>
    {
        public RecordingValidator(List<string> calls)
        {
            RuleFor(request => request)
                .Custom((_, _) => calls.Add("Validator"));
        }
    }
}
