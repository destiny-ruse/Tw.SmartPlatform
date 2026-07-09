using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tw.AspNetCore.Mvc.DynamicProxy;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.DynamicProxy;

/// <summary>验证 PageInterceptionFilterTests 相关行为</summary>
public class PageInterceptionFilterTests
{
    /// <summary>验证 OnPageHandlerExecutionAsync_WithSelectedInterceptor_InvokesPipelineAndWritesModifiedArgumentsToHandler 场景</summary>
    /// <returns>OnPageHandlerExecutionAsync_WithSelectedInterceptor_InvokesPipelineAndWritesModifiedArgumentsToHandler 的执行结果</returns>
    [Fact]
    public async Task OnPageHandlerExecutionAsync_WithSelectedInterceptor_InvokesPipelineAndWritesModifiedArgumentsToHandler()
    {
        RewriteArgumentInterceptor.WasCalled = false;
        var services = CreateServices([typeof(RewriteArgumentInterceptor)]);
        services.AddSingleton<RewriteArgumentInterceptor>();
        using var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<TwPageInterceptionFilter>();
        var model = new SamplePageModel();
        var handlerArguments = new Dictionary<string, object?> { ["value"] = "original" };
        var executingContext = CreateExecutingContext(handlerArguments, model);
        object? valueSeenByNext = null;

        await filter.OnPageHandlerExecutionAsync(
            executingContext,
            () =>
            {
                valueSeenByNext = handlerArguments["value"];

                return CreateExecutedContext(executingContext, new OkResult());
            });

        valueSeenByNext.Should().Be("rewritten");
        handlerArguments["value"].Should().Be("rewritten");
        RewriteArgumentInterceptor.WasCalled.Should().BeTrue();
    }

    /// <summary>验证 OnPageHandlerExecutionAsync_WithoutSelectedInterceptor_CallsNextAndDoesNotInvokePipeline 场景</summary>
    /// <returns>OnPageHandlerExecutionAsync_WithoutSelectedInterceptor_CallsNextAndDoesNotInvokePipeline 的执行结果</returns>
    [Fact]
    public async Task OnPageHandlerExecutionAsync_WithoutSelectedInterceptor_CallsNextAndDoesNotInvokePipeline()
    {
        var selector = new FixedInterceptorSelector([]);
        var pipeline = new CountingPipeline();
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorPipeline>(pipeline);
        using var provider = services.BuildServiceProvider();
        var filter = new TwPageInterceptionFilter(provider, selector);
        var executingContext = CreateExecutingContext(
            new Dictionary<string, object?> { ["value"] = "original" },
            new SamplePageModel());
        var nextCalled = false;

        await filter.OnPageHandlerExecutionAsync(
            executingContext,
            () =>
            {
                nextCalled = true;

                return CreateExecutedContext(executingContext, new OkResult());
            });

        nextCalled.Should().BeTrue();
        pipeline.InvokeCount.Should().Be(0);
    }

    /// <summary>验证 OnPageHandlerExecutionAsync_WhenHandlerMethodMissing_CallsNextWithoutSelection 场景</summary>
    /// <returns>OnPageHandlerExecutionAsync_WhenHandlerMethodMissing_CallsNextWithoutSelection 的执行结果</returns>
    [Fact]
    public async Task OnPageHandlerExecutionAsync_WhenHandlerMethodMissing_CallsNextWithoutSelection()
    {
        var pipeline = new CountingPipeline();
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorPipeline>(pipeline);
        using var provider = services.BuildServiceProvider();
        var filter = new TwPageInterceptionFilter(
            provider,
            new ThrowingInterceptorSelector());
        var executingContext = new PageHandlerExecutingContext(
            CreatePageContext(),
            [],
            handlerMethod: null,
            new Dictionary<string, object?>(),
            new SamplePageModel());
        var nextCalled = false;

        await filter.OnPageHandlerExecutionAsync(
            executingContext,
            () =>
            {
                nextCalled = true;

                return CreateExecutedContext(executingContext, new OkResult());
            });

        nextCalled.Should().BeTrue();
        pipeline.InvokeCount.Should().Be(0);
    }

    /// <summary>验证 OnPageHandlerExecutionAsync_WhenInterceptorShortCircuits_SetsExecutingResultAndDoesNotCallNext 场景</summary>
    /// <returns>OnPageHandlerExecutionAsync_WhenInterceptorShortCircuits_SetsExecutingResultAndDoesNotCallNext 的执行结果</returns>
    [Fact]
    public async Task OnPageHandlerExecutionAsync_WhenInterceptorShortCircuits_SetsExecutingResultAndDoesNotCallNext()
    {
        var services = CreateServices([typeof(ShortCircuitInterceptor)]);
        services.AddSingleton<ShortCircuitInterceptor>();
        using var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<TwPageInterceptionFilter>();
        var executingContext = CreateExecutingContext(
            new Dictionary<string, object?> { ["value"] = "original" },
            new SamplePageModel());
        var nextCalled = false;

        await filter.OnPageHandlerExecutionAsync(
            executingContext,
            () =>
            {
                nextCalled = true;

                return CreateExecutedContext(executingContext, new BadRequestResult());
            });

        nextCalled.Should().BeFalse();
        executingContext.Result.Should().BeSameAs(ShortCircuitInterceptor.Result);
    }

    /// <summary>验证 OnPageHandlerExecutionAsync_WithAttributeSelectorAndHandlerInterceptAttribute_InvokesInterceptor 场景</summary>
    /// <returns>OnPageHandlerExecutionAsync_WithAttributeSelectorAndHandlerInterceptAttribute_InvokesInterceptor 的执行结果</returns>
    [Fact]
    public async Task OnPageHandlerExecutionAsync_WithAttributeSelectorAndHandlerInterceptAttribute_InvokesInterceptor()
    {
        AttributeRecordingInterceptor.CallCount = 0;
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorSelector, AttributeInterceptorSelector>();
        services.AddSingleton<IInterceptorPipeline, InterceptorPipeline>();
        services.AddTransient<TwPageInterceptionFilter>();
        services.AddSingleton<AttributeRecordingInterceptor>();
        using var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<TwPageInterceptionFilter>();
        var model = new InterceptedPageModel();
        var executingContext = CreateExecutingContext(
            new Dictionary<string, object?> { ["value"] = "original" },
            model,
            CreateHandlerMethod<InterceptedPageModel>(nameof(InterceptedPageModel.OnGet)));

        await filter.OnPageHandlerExecutionAsync(
            executingContext,
            () => CreateExecutedContext(executingContext, new OkResult()));

        AttributeRecordingInterceptor.CallCount.Should().Be(1);
    }

    /// <summary>验证 AddMvcIntegration_RegistersPageInterceptionFilter 场景</summary>
    [Fact]
    public void AddMvcIntegration_RegistersPageInterceptionFilter()
    {
        var services = new ServiceCollection();

        services.AddMvcIntegration();

        using var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        mvcOptions.Filters.Count(IsPageInterceptionFilter).Should().Be(1);
    }

    /// <summary>验证 AddMvcIntegration_WhenCalledTwice_RegistersPageInterceptionFilterOnce 场景</summary>
    [Fact]
    public void AddMvcIntegration_WhenCalledTwice_RegistersPageInterceptionFilterOnce()
    {
        var services = new ServiceCollection();

        services.AddMvcIntegration();
        services.AddMvcIntegration();

        using var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        mvcOptions.Filters.Count(IsPageInterceptionFilter).Should().Be(1);
    }

    /// <summary>验证 CreateServices 场景</summary>
    /// <param name="interceptorTypes">interceptorTypes 参数</param>
    /// <returns>CreateServices 的执行结果</returns>
    private static ServiceCollection CreateServices(IReadOnlyList<Type> interceptorTypes)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorSelector>(new FixedInterceptorSelector(interceptorTypes));
        services.AddSingleton<IInterceptorPipeline, InterceptorPipeline>();
        services.AddTransient<TwPageInterceptionFilter>();

        return services;
    }

    /// <summary>验证 CreatePageContext 场景</summary>
    /// <returns>CreatePageContext 的执行结果</returns>
    private static PageContext CreatePageContext() =>
        new(new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new CompiledPageActionDescriptor { DisplayName = "/Sample" }));

    /// <summary>验证 CreateHandlerMethod 场景</summary>
    /// <typeparam name="TPageModel">TPageModel 类型参数</typeparam>
    /// <param name="name">name 参数</param>
    /// <returns>CreateHandlerMethod 的执行结果</returns>
    private static HandlerMethodDescriptor CreateHandlerMethod<TPageModel>(string name) =>
        new()
        {
            MethodInfo = typeof(TPageModel).GetMethod(name)!,
            Name = name,
        };

    /// <summary>验证 CreateExecutingContext 场景</summary>
    /// <param name="handlerArguments">handlerArguments 参数</param>
    /// <param name="handlerInstance">handlerInstance 参数</param>
    /// <param name="handlerMethod">handlerMethod 参数</param>
    /// <returns>CreateExecutingContext 的执行结果</returns>
    private static PageHandlerExecutingContext CreateExecutingContext(
        IDictionary<string, object?> handlerArguments,
        object handlerInstance,
        HandlerMethodDescriptor? handlerMethod = null) =>
        new(
            CreatePageContext(),
            [],
            handlerMethod ?? CreateHandlerMethod<SamplePageModel>(nameof(SamplePageModel.OnGet)),
            handlerArguments,
            handlerInstance);

    /// <summary>验证 CreateExecutedContext 场景</summary>
    /// <param name="executingContext">executingContext 参数</param>
    /// <param name="result">result 参数</param>
    /// <returns>CreateExecutedContext 的执行结果</returns>
    private static Task<PageHandlerExecutedContext> CreateExecutedContext(
        PageHandlerExecutingContext executingContext,
        IActionResult? result = null) =>
        Task.FromResult(new PageHandlerExecutedContext(
            CreatePageContext(),
            [],
            executingContext.HandlerMethod!,
            executingContext.HandlerInstance)
        {
            Result = result,
        });

    /// <summary>验证 IsPageInterceptionFilter 场景</summary>
    /// <param name="filter">filter 参数</param>
    /// <returns>IsPageInterceptionFilter 的执行结果</returns>
    private static bool IsPageInterceptionFilter(IFilterMetadata filter) =>
        filter is TypeFilterAttribute typeFilter
        && typeFilter.ImplementationType == typeof(TwPageInterceptionFilter);

    /// <summary>验证 FixedInterceptorSelector 相关行为</summary>
    private sealed class FixedInterceptorSelector(IReadOnlyList<Type> interceptorTypes) : IInterceptorSelector
    {
        /// <summary>验证 SelectInterceptors 场景</summary>
        /// <param name="implementationType">implementationType 参数</param>
        /// <param name="serviceType">serviceType 参数</param>
        /// <param name="method">method 参数</param>
        /// <returns>SelectInterceptors 的执行结果</returns>
        public IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, MethodInfo method) =>
            interceptorTypes;
    }

    /// <summary>验证 ThrowingInterceptorSelector 相关行为</summary>
    private sealed class ThrowingInterceptorSelector : IInterceptorSelector
    {
        /// <summary>验证 SelectInterceptors 场景</summary>
        /// <param name="implementationType">implementationType 参数</param>
        /// <param name="serviceType">serviceType 参数</param>
        /// <param name="method">method 参数</param>
        /// <returns>SelectInterceptors 的执行结果</returns>
        public IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, MethodInfo method) =>
            throw new InvalidOperationException("selector should not run when handler method is missing");
    }

    /// <summary>验证 CountingPipeline 相关行为</summary>
    private sealed class CountingPipeline : IInterceptorPipeline
    {
        /// <summary>表示 InvokeCount 属性</summary>
        public int InvokeCount { get; private set; }

        /// <summary>验证 InvokeAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <param name="interceptors">interceptors 参数</param>
        /// <returns>InvokeAsync 的执行结果</returns>
        public ValueTask InvokeAsync(IInvocationContext context, IReadOnlyList<IInterceptor> interceptors)
        {
            InvokeCount++;

            return context.ProceedAsync();
        }
    }

    /// <summary>验证 SamplePageModel 相关行为</summary>
    private sealed class SamplePageModel : PageModel
    {
        /// <summary>验证 OnGet 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>OnGet 的执行结果</returns>
        public IActionResult OnGet(string value) => new OkObjectResult(value);
    }

    /// <summary>验证 InterceptedPageModel 相关行为</summary>
    private sealed class InterceptedPageModel : PageModel
    {
        /// <summary>验证 OnGet 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>OnGet 的执行结果</returns>
        [Intercept(typeof(AttributeRecordingInterceptor))]
        public IActionResult OnGet(string value) => new OkObjectResult(value);
    }

    /// <summary>验证 RewriteArgumentInterceptor 相关行为</summary>
    private sealed class RewriteArgumentInterceptor : IInterceptor
    {
        /// <summary>表示 WasCalled 属性</summary>
        public static bool WasCalled { get; set; }

        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            WasCalled = true;
            context.Arguments[0] = "rewritten";

            await context.ProceedAsync().ConfigureAwait(false);
        }
    }

    /// <summary>验证 AttributeRecordingInterceptor 相关行为</summary>
    private sealed class AttributeRecordingInterceptor : IInterceptor
    {
        /// <summary>表示 CallCount 属性</summary>
        public static int CallCount { get; set; }

        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            CallCount++;

            await context.ProceedAsync().ConfigureAwait(false);
        }
    }

    /// <summary>验证 ShortCircuitInterceptor 相关行为</summary>
    private sealed class ShortCircuitInterceptor : IInterceptor
    {
        /// <summary>表示 Result 字段</summary>
        public static readonly OkResult Result = new();

        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context)
        {
            context.ReturnValue = Result;

            return ValueTask.CompletedTask;
        }
    }
}
