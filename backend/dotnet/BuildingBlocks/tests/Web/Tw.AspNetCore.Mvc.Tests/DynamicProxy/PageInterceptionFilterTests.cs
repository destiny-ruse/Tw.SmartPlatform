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

public class PageInterceptionFilterTests
{
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

    [Fact]
    public void AddMvcIntegration_RegistersPageInterceptionFilter()
    {
        var services = new ServiceCollection();

        services.AddMvcIntegration();

        using var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        mvcOptions.Filters.Count(IsPageInterceptionFilter).Should().Be(1);
    }

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

    private static ServiceCollection CreateServices(IReadOnlyList<Type> interceptorTypes)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorSelector>(new FixedInterceptorSelector(interceptorTypes));
        services.AddSingleton<IInterceptorPipeline, InterceptorPipeline>();
        services.AddTransient<TwPageInterceptionFilter>();

        return services;
    }

    private static PageContext CreatePageContext() =>
        new(new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new CompiledPageActionDescriptor { DisplayName = "/Sample" }));

    private static HandlerMethodDescriptor CreateHandlerMethod<TPageModel>(string name) =>
        new()
        {
            MethodInfo = typeof(TPageModel).GetMethod(name)!,
            Name = name,
        };

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

    private static bool IsPageInterceptionFilter(IFilterMetadata filter) =>
        filter is TypeFilterAttribute typeFilter
        && typeFilter.ImplementationType == typeof(TwPageInterceptionFilter);

    private sealed class FixedInterceptorSelector(IReadOnlyList<Type> interceptorTypes) : IInterceptorSelector
    {
        public IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, MethodInfo method) =>
            interceptorTypes;
    }

    private sealed class ThrowingInterceptorSelector : IInterceptorSelector
    {
        public IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, MethodInfo method) =>
            throw new InvalidOperationException("selector should not run when handler method is missing");
    }

    private sealed class CountingPipeline : IInterceptorPipeline
    {
        public int InvokeCount { get; private set; }

        public ValueTask InvokeAsync(IInvocationContext context, IReadOnlyList<IInterceptor> interceptors)
        {
            InvokeCount++;

            return context.ProceedAsync();
        }
    }

    private sealed class SamplePageModel : PageModel
    {
        public IActionResult OnGet(string value) => new OkObjectResult(value);
    }

    private sealed class InterceptedPageModel : PageModel
    {
        [Intercept(typeof(AttributeRecordingInterceptor))]
        public IActionResult OnGet(string value) => new OkObjectResult(value);
    }

    private sealed class RewriteArgumentInterceptor : IInterceptor
    {
        public static bool WasCalled { get; set; }

        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            WasCalled = true;
            context.Arguments[0] = "rewritten";

            await context.ProceedAsync().ConfigureAwait(false);
        }
    }

    private sealed class AttributeRecordingInterceptor : IInterceptor
    {
        public static int CallCount { get; set; }

        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            CallCount++;

            await context.ProceedAsync().ConfigureAwait(false);
        }
    }

    private sealed class ShortCircuitInterceptor : IInterceptor
    {
        public static readonly OkResult Result = new();

        public ValueTask InterceptAsync(IInvocationContext context)
        {
            context.ReturnValue = Result;

            return ValueTask.CompletedTask;
        }
    }
}
