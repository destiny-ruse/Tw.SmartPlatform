using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tw.AspNetCore.Mvc;
using Tw.AspNetCore.Mvc.Context;
using Tw.AspNetCore.Mvc.DynamicProxy;
using Tw.Threading;
using Tw.DependencyInjection.DynamicProxy;
using Tw.DynamicProxy.Abstractions;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.DynamicProxy;

public class MvcInterceptionFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_WithSelectedInterceptor_InvokesPipelineAndWritesModifiedArgumentsToAction()
    {
        RewriteArgumentInterceptor.WasCalled = false;
        var services = CreateServices([typeof(RewriteArgumentInterceptor)]);
        services.AddSingleton<RewriteArgumentInterceptor>();
        using var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<TwActionInterceptionFilter>();
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.Echo));
        var actionArguments = new Dictionary<string, object?>
        {
            ["value"] = "original",
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);
        object? valueSeenByNext = null;

        await filter.OnActionExecutionAsync(
            executingContext,
            () =>
            {
                valueSeenByNext = actionArguments["value"];

                return CreateExecutedContext(actionContext, controller, new OkResult());
            });

        valueSeenByNext.Should().Be("rewritten");
        actionArguments["value"].Should().Be("rewritten");
        RewriteArgumentInterceptor.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithoutSelectedInterceptor_CallsNextAndDoesNotInvokePipeline()
    {
        var selector = new FixedInterceptorSelector([]);
        var pipeline = new CountingPipeline();
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorPipeline>(pipeline);
        using var provider = services.BuildServiceProvider();
        var filter = new TwActionInterceptionFilter(provider, selector);
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.Echo));
        var executingContext = CreateExecutingContext(
            actionContext,
            new Dictionary<string, object?> { ["value"] = "original" },
            controller);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(
            executingContext,
            () =>
            {
                nextCalled = true;

                return CreateExecutedContext(actionContext, controller, new OkResult());
            });

        nextCalled.Should().BeTrue();
        pipeline.InvokeCount.Should().Be(0);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithoutSelectedInterceptor_DoesNotResolvePipeline()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorSelector>(new FixedInterceptorSelector([]));
        services.AddSingleton<IInterceptorPipeline>(_ =>
            throw new InvalidOperationException("pipeline should not be resolved"));
        services.AddTransient<TwActionInterceptionFilter>();
        using var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<TwActionInterceptionFilter>();
        var controller = new SampleController();
        var actionContext = CreateActionContext<SampleController>(nameof(SampleController.Echo));
        var executingContext = CreateExecutingContext(
            actionContext,
            new Dictionary<string, object?> { ["value"] = "original" },
            controller);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(
            executingContext,
            () =>
            {
                nextCalled = true;

                return CreateExecutedContext(actionContext, controller, new OkResult());
            });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenInterceptorShortCircuits_SetsExecutingResultAndDoesNotCallNext()
    {
        var services = CreateServices([typeof(ShortCircuitInterceptor)]);
        services.AddSingleton<ShortCircuitInterceptor>();
        using var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<TwActionInterceptionFilter>();
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.Echo));
        var executingContext = CreateExecutingContext(
            actionContext,
            new Dictionary<string, object?> { ["value"] = "original" },
            controller);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(
            executingContext,
            () =>
            {
                nextCalled = true;

                return CreateExecutedContext(actionContext, controller, new BadRequestResult());
            });

        nextCalled.Should().BeFalse();
        executingContext.Result.Should().BeSameAs(ShortCircuitInterceptor.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenInterceptorReplacesResultAfterProceed_UpdatesExecutedContextResult()
    {
        var services = CreateServices([typeof(ReplaceResultAfterProceedInterceptor)]);
        services.AddSingleton<ReplaceResultAfterProceedInterceptor>();
        using var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<TwActionInterceptionFilter>();
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.Echo));
        var executingContext = CreateExecutingContext(
            actionContext,
            new Dictionary<string, object?> { ["value"] = "original" },
            controller);
        var executedContext = new ActionExecutedContext(actionContext, [], controller)
        {
            Result = new OkObjectResult("original"),
        };

        await filter.OnActionExecutionAsync(executingContext, () => Task.FromResult(executedContext));

        executedContext.Result.Should().BeSameAs(ReplaceResultAfterProceedInterceptor.Result);
        executingContext.Result.Should().BeSameAs(ReplaceResultAfterProceedInterceptor.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenInterceptorConvertsExceptionToResult_MarksExceptionHandled()
    {
        var services = CreateServices([typeof(ConvertExceptionToResultInterceptor)]);
        services.AddSingleton<ConvertExceptionToResultInterceptor>();
        using var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<TwActionInterceptionFilter>();
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.Echo));
        var executingContext = CreateExecutingContext(
            actionContext,
            new Dictionary<string, object?> { ["value"] = "original" },
            controller);
        var executedContext = new ActionExecutedContext(actionContext, [], controller)
        {
            Exception = new InvalidOperationException("action failed"),
            ExceptionHandled = false,
        };

        await filter.OnActionExecutionAsync(executingContext, () => Task.FromResult(executedContext));

        executedContext.ExceptionHandled.Should().BeTrue();
        executedContext.Result.Should().BeSameAs(ConvertExceptionToResultInterceptor.Result);
        executingContext.Result.Should().BeSameAs(ConvertExceptionToResultInterceptor.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithAttributeSelectorAndMethodInterceptAttribute_InvokesInterceptor()
    {
        AttributeRecordingInterceptor.CallCount = 0;
        var services = CreateAttributeSelectorServices();
        services.AddSingleton<AttributeRecordingInterceptor>();
        using var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<TwActionInterceptionFilter>();
        var controller = new MethodInterceptController();
        var actionContext = CreateActionContext<MethodInterceptController>(nameof(MethodInterceptController.Echo));
        var executingContext = CreateExecutingContext(
            actionContext,
            new Dictionary<string, object?> { ["value"] = "original" },
            controller);

        await filter.OnActionExecutionAsync(
            executingContext,
            () => CreateExecutedContext(actionContext, controller, new OkResult()));

        AttributeRecordingInterceptor.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithAttributeSelectorAndControllerInterceptAttribute_InvokesInterceptor()
    {
        AttributeRecordingInterceptor.CallCount = 0;
        var services = CreateAttributeSelectorServices();
        services.AddSingleton<AttributeRecordingInterceptor>();
        using var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<TwActionInterceptionFilter>();
        var controller = new ControllerInterceptController();
        var actionContext = CreateActionContext<ControllerInterceptController>(nameof(ControllerInterceptController.Echo));
        var executingContext = CreateExecutingContext(
            actionContext,
            new Dictionary<string, object?> { ["value"] = "original" },
            controller);

        await filter.OnActionExecutionAsync(
            executingContext,
            () => CreateExecutedContext(actionContext, controller, new OkResult()));

        AttributeRecordingInterceptor.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithAttributeSelectorAndMethodDisableInterception_DoesNotInvokeInterceptor()
    {
        AttributeRecordingInterceptor.CallCount = 0;
        var services = CreateAttributeSelectorServices();
        services.AddSingleton<AttributeRecordingInterceptor>();
        using var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<TwActionInterceptionFilter>();
        var controller = new MethodDisableInterceptionController();
        var actionContext = CreateActionContext<MethodDisableInterceptionController>(
            nameof(MethodDisableInterceptionController.Echo));
        var executingContext = CreateExecutingContext(
            actionContext,
            new Dictionary<string, object?> { ["value"] = "original" },
            controller);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(
            executingContext,
            () =>
            {
                nextCalled = true;

                return CreateExecutedContext(actionContext, controller, new OkResult());
            });

        nextCalled.Should().BeTrue();
        AttributeRecordingInterceptor.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithAttributeSelectorAndControllerDisableInterception_DoesNotInvokeInterceptor()
    {
        AttributeRecordingInterceptor.CallCount = 0;
        var services = CreateAttributeSelectorServices();
        services.AddSingleton<AttributeRecordingInterceptor>();
        using var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<TwActionInterceptionFilter>();
        var controller = new ControllerDisableInterceptionController();
        var actionContext = CreateActionContext<ControllerDisableInterceptionController>(
            nameof(ControllerDisableInterceptionController.Echo));
        var executingContext = CreateExecutingContext(
            actionContext,
            new Dictionary<string, object?> { ["value"] = "original" },
            controller);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(
            executingContext,
            () =>
            {
                nextCalled = true;

                return CreateExecutedContext(actionContext, controller, new OkResult());
            });

        nextCalled.Should().BeTrue();
        AttributeRecordingInterceptor.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenSelectedTypeDoesNotImplementInterceptor_ThrowsClearInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorSelector>(new FixedInterceptorSelector([typeof(NonInterceptor)]));
        services.AddSingleton<IInterceptorPipeline>(_ =>
            throw new InvalidOperationException("pipeline should not be resolved"));
        using var provider = services.BuildServiceProvider();
        var filter = new TwActionInterceptionFilter(provider, provider.GetRequiredService<IInterceptorSelector>());
        var controller = new SampleController();
        var actionContext = CreateActionContext<SampleController>(nameof(SampleController.Echo));
        var executingContext = CreateExecutingContext(
            actionContext,
            new Dictionary<string, object?> { ["value"] = "original" },
            controller);

        var act = async () => await filter.OnActionExecutionAsync(
            executingContext,
            () => CreateExecutedContext(actionContext, controller, new OkResult()));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*必须实现*{typeof(IInterceptor).FullName}*");
    }

    [Fact]
    public void AddMvcIntegration_ReturnsSameServicesAndRegistersMvcFilterAndCancellationTokenProvider()
    {
        var services = new ServiceCollection();

        var result = services.AddMvcIntegration();

        result.Should().BeSameAs(services);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IHttpContextAccessor>().Should().NotBeNull();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Should().BeOfType<HttpContextCancellationTokenProvider>();

        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        var hasActionInterceptionFilter = mvcOptions.Filters.Any(IsActionInterceptionFilter);
        hasActionInterceptionFilter.Should().BeTrue();
    }

    [Fact]
    public void AddMvcIntegration_WhenCalledTwice_RegistersActionInterceptionFilterOnce()
    {
        var services = new ServiceCollection();

        services.AddMvcIntegration();
        services.AddMvcIntegration();

        using var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        mvcOptions.Filters.Count(IsActionInterceptionFilter).Should().Be(1);
    }

    private static ServiceCollection CreateServices(IReadOnlyList<Type> interceptorTypes)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorSelector>(new FixedInterceptorSelector(interceptorTypes));
        services.AddSingleton<IInterceptorPipeline, InterceptorPipeline>();
        services.AddTransient<TwActionInterceptionFilter>();

        return services;
    }

    private static ServiceCollection CreateAttributeSelectorServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorSelector, AttributeInterceptorSelector>();
        services.AddSingleton<IInterceptorPipeline, InterceptorPipeline>();
        services.AddTransient<TwActionInterceptionFilter>();

        return services;
    }

    private static ActionContext CreateActionContext(string actionName) =>
        CreateActionContext<SampleController>(actionName);

    private static ActionContext CreateActionContext<TController>(string actionName)
    {
        var controllerType = typeof(TController);
        var method = controllerType.GetMethod(actionName)!;
        var actionDescriptor = new ControllerActionDescriptor
        {
            ActionName = actionName,
            ControllerName = controllerType.Name,
            ControllerTypeInfo = controllerType.GetTypeInfo(),
            DisplayName = actionName,
            MethodInfo = method,
        };

        return new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);
    }

    private static ActionExecutingContext CreateExecutingContext(
        ActionContext actionContext,
        IDictionary<string, object?> actionArguments,
        object controller) =>
        new(actionContext, [], actionArguments, controller);

    private static Task<ActionExecutedContext> CreateExecutedContext(
        ActionContext actionContext,
        object controller,
        IActionResult? result = null) =>
        Task.FromResult(new ActionExecutedContext(actionContext, [], controller)
        {
            Result = result,
        });

    private static bool IsActionInterceptionFilter(IFilterMetadata filter) =>
        filter is TypeFilterAttribute typeFilter
        && typeFilter.ImplementationType == typeof(TwActionInterceptionFilter);

    private sealed class FixedInterceptorSelector(IReadOnlyList<Type> interceptorTypes) : IInterceptorSelector
    {
        public IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, MethodInfo method) =>
            interceptorTypes;
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

    private sealed class SampleController
    {
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    private sealed class MethodInterceptController
    {
        [Intercept(typeof(AttributeRecordingInterceptor))]
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    [Intercept(typeof(AttributeRecordingInterceptor))]
    private sealed class ControllerInterceptController
    {
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    [Intercept(typeof(AttributeRecordingInterceptor))]
    private sealed class MethodDisableInterceptionController
    {
        [DisableInterception]
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    [Intercept(typeof(AttributeRecordingInterceptor))]
    [DisableInterception]
    private sealed class ControllerDisableInterceptionController
    {
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    private sealed class NonInterceptor;

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

    private sealed class ReplaceResultAfterProceedInterceptor : IInterceptor
    {
        public static readonly OkObjectResult Result = new("replacement");

        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            await context.ProceedAsync().ConfigureAwait(false);

            context.ReturnValue = Result;
        }
    }

    private sealed class ConvertExceptionToResultInterceptor : IInterceptor
    {
        public static readonly OkResult Result = new();

        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            try
            {
                await context.ProceedAsync().ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                context.ReturnValue = Result;
            }
        }
    }
}
