using AwesomeAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tw.Application.Pipeline;
using Xunit;

namespace Tw.Application.Tests.Pipeline;

/// <summary>
/// 覆盖Application管道MediatRIntegration的核心行为和边界条件
/// </summary>
public sealed class ApplicationPipelineMediatRIntegrationTests
{
    /// <summary>
    /// 验证添加Application管道ExecutesMediatRRequestsThroughOrderedApplication管道
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证添加Application管道RunsFluentValidatorsAtValidation管道Position
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 封装示例请求相关的数据和行为
    /// </summary>
    private sealed record SampleRequest : IRequest<string>;

    /// <summary>
    /// 覆盖示例请求处理器的核心行为和边界条件
    /// </summary>
    private sealed class SampleRequestHandler(List<string> calls) : IRequestHandler<SampleRequest, string>
    {
        /// <summary>
        /// 处理测试请求并返回响应
        /// </summary>
        /// <param name="request">用于提供请求</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的string</returns>
        public Task<string> Handle(SampleRequest request, CancellationToken cancellationToken)
        {
            calls.Add("Handler");
            return Task.FromResult("handled");
        }
    }

    /// <summary>
    /// 覆盖Recording行为的核心行为和边界条件
    /// </summary>
    private sealed class RecordingBehavior(string name, List<string> calls) : IApplicationPipelineBehavior
    {
        /// <summary>
        /// 名称在当前对象中的业务含义
        /// </summary>
        public string Name => name;

        /// <summary>
        /// 执行测试管道委托并记录调用
        /// </summary>
        /// <param name="next">用于提供next</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public async Task InvokeAsync(Func<Task> next, CancellationToken cancellationToken)
        {
            calls.Add($"{name}-before");
            await next();
            calls.Add($"{name}-after");
        }
    }

    /// <summary>
    /// 覆盖RecordingCompletedHook的核心行为和边界条件
    /// </summary>
    private sealed class RecordingCompletedHook(List<string> calls) : ICompletedHook
    {
        /// <summary>
        /// 运行测试管道委托
        /// </summary>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task RunAsync(CancellationToken cancellationToken)
        {
            calls.Add("CompletedHook");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 覆盖RecordingValidator的核心行为和边界条件
    /// </summary>
    private sealed class RecordingValidator : AbstractValidator<SampleRequest>
    {
        /// <summary>
        /// 初始化 RecordingValidator 实例
        /// </summary>
        /// <param name="calls">用于提供calls</param>
        public RecordingValidator(List<string> calls)
        {
            RuleFor(request => request)
                .Custom((_, _) => calls.Add("Validator"));
        }
    }
}
