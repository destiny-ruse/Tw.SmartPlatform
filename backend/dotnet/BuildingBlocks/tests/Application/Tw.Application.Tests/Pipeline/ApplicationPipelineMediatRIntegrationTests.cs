using AwesomeAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tw.Application.Pipeline;
using Xunit;

namespace Tw.Application.Tests.Pipeline;

/// <summary>验证 ApplicationPipelineMediatRIntegrationTests 相关行为</summary>
public sealed class ApplicationPipelineMediatRIntegrationTests
{
    /// <summary>验证 AddApplicationPipeline_ExecutesMediatRRequestsThroughOrderedApplicationPipeline 场景</summary>
    /// <returns>AddApplicationPipeline_ExecutesMediatRRequestsThroughOrderedApplicationPipeline 的执行结果</returns>
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

    /// <summary>验证 AddApplicationPipeline_RunsFluentValidatorsAtValidationPipelinePosition 场景</summary>
    /// <returns>AddApplicationPipeline_RunsFluentValidatorsAtValidationPipelinePosition 的执行结果</returns>
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

    /// <summary>表示 SampleRequest 声明</summary>
    private sealed record SampleRequest : IRequest<string>;

    /// <summary>验证 SampleRequestHandler 相关行为</summary>
    private sealed class SampleRequestHandler(List<string> calls) : IRequestHandler<SampleRequest, string>
    {
        /// <summary>验证 Handle 场景</summary>
        /// <param name="request">request 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>Handle 的执行结果</returns>
        public Task<string> Handle(SampleRequest request, CancellationToken cancellationToken)
        {
            calls.Add("Handler");
            return Task.FromResult("handled");
        }
    }

    /// <summary>验证 RecordingBehavior 相关行为</summary>
    private sealed class RecordingBehavior(string name, List<string> calls) : IApplicationPipelineBehavior
    {
        /// <summary>表示 Name 属性</summary>
        public string Name => name;

        /// <summary>验证 InvokeAsync 场景</summary>
        /// <param name="next">next 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>InvokeAsync 的执行结果</returns>
        public async Task InvokeAsync(Func<Task> next, CancellationToken cancellationToken)
        {
            calls.Add($"{name}-before");
            await next();
            calls.Add($"{name}-after");
        }
    }

    /// <summary>验证 RecordingCompletedHook 相关行为</summary>
    private sealed class RecordingCompletedHook(List<string> calls) : ICompletedHook
    {
        /// <summary>验证 RunAsync 场景</summary>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>RunAsync 的执行结果</returns>
        public Task RunAsync(CancellationToken cancellationToken)
        {
            calls.Add("CompletedHook");
            return Task.CompletedTask;
        }
    }

    /// <summary>验证 RecordingValidator 相关行为</summary>
    private sealed class RecordingValidator : AbstractValidator<SampleRequest>
    {
        /// <summary>初始化 RecordingValidator 实例</summary>
        /// <param name="calls">calls 参数</param>
        public RecordingValidator(List<string> calls)
        {
            RuleFor(request => request)
                .Custom((_, _) => calls.Add("Validator"));
        }
    }
}
