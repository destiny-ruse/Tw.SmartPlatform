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
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.DynamicProxy;

/// <summary>验证 MvcInterceptionFilterTests 相关行为</summary>
public class MvcInterceptionFilterTests
{
    /// <summary>验证 OnActionExecutionAsync_WithSelectedInterceptor_InvokesPipelineAndWritesModifiedArgumentsToAction 场景</summary>
    /// <returns>OnActionExecutionAsync_WithSelectedInterceptor_InvokesPipelineAndWritesModifiedArgumentsToAction 的执行结果</returns>
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

    /// <summary>验证 OnActionExecutionAsync_WithoutSelectedInterceptor_CallsNextAndDoesNotInvokePipeline 场景</summary>
    /// <returns>OnActionExecutionAsync_WithoutSelectedInterceptor_CallsNextAndDoesNotInvokePipeline 的执行结果</returns>
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

    /// <summary>验证 OnActionExecutionAsync_WithoutSelectedInterceptor_DoesNotResolvePipeline 场景</summary>
    /// <returns>OnActionExecutionAsync_WithoutSelectedInterceptor_DoesNotResolvePipeline 的执行结果</returns>
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

    /// <summary>验证 OnActionExecutionAsync_WhenInterceptorShortCircuits_SetsExecutingResultAndDoesNotCallNext 场景</summary>
    /// <returns>OnActionExecutionAsync_WhenInterceptorShortCircuits_SetsExecutingResultAndDoesNotCallNext 的执行结果</returns>
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

    /// <summary>验证 OnActionExecutionAsync_WhenInterceptorReplacesResultAfterProceed_UpdatesExecutedContextResult 场景</summary>
    /// <returns>OnActionExecutionAsync_WhenInterceptorReplacesResultAfterProceed_UpdatesExecutedContextResult 的执行结果</returns>
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

    /// <summary>验证 OnActionExecutionAsync_WhenInterceptorConvertsExceptionToResult_MarksExceptionHandled 场景</summary>
    /// <returns>OnActionExecutionAsync_WhenInterceptorConvertsExceptionToResult_MarksExceptionHandled 的执行结果</returns>
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

    /// <summary>验证 OnActionExecutionAsync_WithAttributeSelectorAndMethodInterceptAttribute_InvokesInterceptor 场景</summary>
    /// <returns>OnActionExecutionAsync_WithAttributeSelectorAndMethodInterceptAttribute_InvokesInterceptor 的执行结果</returns>
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

    /// <summary>验证 OnActionExecutionAsync_WithAttributeSelectorAndControllerInterceptAttribute_InvokesInterceptor 场景</summary>
    /// <returns>OnActionExecutionAsync_WithAttributeSelectorAndControllerInterceptAttribute_InvokesInterceptor 的执行结果</returns>
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

    /// <summary>验证 OnActionExecutionAsync_WithAttributeSelectorAndMethodDisableInterception_DoesNotInvokeInterceptor 场景</summary>
    /// <returns>OnActionExecutionAsync_WithAttributeSelectorAndMethodDisableInterception_DoesNotInvokeInterceptor 的执行结果</returns>
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

    /// <summary>验证 OnActionExecutionAsync_WithAttributeSelectorAndControllerDisableInterception_DoesNotInvokeInterceptor 场景</summary>
    /// <returns>OnActionExecutionAsync_WithAttributeSelectorAndControllerDisableInterception_DoesNotInvokeInterceptor 的执行结果</returns>
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

    /// <summary>验证 OnActionExecutionAsync_WhenSelectedTypeDoesNotImplementInterceptor_ThrowsClearInvalidOperationException 场景</summary>
    /// <returns>OnActionExecutionAsync_WhenSelectedTypeDoesNotImplementInterceptor_ThrowsClearInvalidOperationException 的执行结果</returns>
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

    /// <summary>验证 AddMvcIntegration_ReturnsSameServicesAndRegistersMvcFilterAndCancellationTokenProvider 场景</summary>
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

    /// <summary>验证 AddMvcIntegration_WhenCalledTwice_RegistersActionInterceptionFilterOnce 场景</summary>
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

    /// <summary>验证 CreateServices 场景</summary>
    /// <param name="interceptorTypes">interceptorTypes 参数</param>
    /// <returns>CreateServices 的执行结果</returns>
    private static ServiceCollection CreateServices(IReadOnlyList<Type> interceptorTypes)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorSelector>(new FixedInterceptorSelector(interceptorTypes));
        services.AddSingleton<IInterceptorPipeline, InterceptorPipeline>();
        services.AddTransient<TwActionInterceptionFilter>();

        return services;
    }

    /// <summary>验证 CreateAttributeSelectorServices 场景</summary>
    /// <returns>CreateAttributeSelectorServices 的执行结果</returns>
    private static ServiceCollection CreateAttributeSelectorServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorSelector, AttributeInterceptorSelector>();
        services.AddSingleton<IInterceptorPipeline, InterceptorPipeline>();
        services.AddTransient<TwActionInterceptionFilter>();

        return services;
    }

    /// <summary>验证 CreateActionContext 场景</summary>
    /// <param name="actionName">actionName 参数</param>
    /// <returns>CreateActionContext 的执行结果</returns>
    private static ActionContext CreateActionContext(string actionName) =>
        CreateActionContext<SampleController>(actionName);

    /// <summary>验证 CreateActionContext 场景</summary>
    /// <typeparam name="TController">TController 类型参数</typeparam>
    /// <param name="actionName">actionName 参数</param>
    /// <returns>CreateActionContext 的执行结果</returns>
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

    /// <summary>验证 CreateExecutingContext 场景</summary>
    /// <param name="actionContext">actionContext 参数</param>
    /// <param name="actionArguments">actionArguments 参数</param>
    /// <param name="controller">controller 参数</param>
    /// <returns>CreateExecutingContext 的执行结果</returns>
    private static ActionExecutingContext CreateExecutingContext(
        ActionContext actionContext,
        IDictionary<string, object?> actionArguments,
        object controller) =>
        new(actionContext, [], actionArguments, controller);

    /// <summary>验证 CreateExecutedContext 场景</summary>
    /// <param name="actionContext">actionContext 参数</param>
    /// <param name="controller">controller 参数</param>
    /// <param name="result">result 参数</param>
    /// <returns>CreateExecutedContext 的执行结果</returns>
    private static Task<ActionExecutedContext> CreateExecutedContext(
        ActionContext actionContext,
        object controller,
        IActionResult? result = null) =>
        Task.FromResult(new ActionExecutedContext(actionContext, [], controller)
        {
            Result = result,
        });

    /// <summary>验证 IsActionInterceptionFilter 场景</summary>
    /// <param name="filter">filter 参数</param>
    /// <returns>IsActionInterceptionFilter 的执行结果</returns>
    private static bool IsActionInterceptionFilter(IFilterMetadata filter) =>
        filter is TypeFilterAttribute typeFilter
        && typeFilter.ImplementationType == typeof(TwActionInterceptionFilter);

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

    /// <summary>验证 SampleController 相关行为</summary>
    private sealed class SampleController
    {
        /// <summary>验证 Echo 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>Echo 的执行结果</returns>
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    /// <summary>验证 MethodInterceptController 相关行为</summary>
    private sealed class MethodInterceptController
    {
        /// <summary>验证 Echo 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>Echo 的执行结果</returns>
        [Intercept(typeof(AttributeRecordingInterceptor))]
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    /// <summary>验证 ControllerInterceptController 相关行为</summary>
    [Intercept(typeof(AttributeRecordingInterceptor))]
    private sealed class ControllerInterceptController
    {
        /// <summary>验证 Echo 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>Echo 的执行结果</returns>
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    /// <summary>验证 MethodDisableInterceptionController 相关行为</summary>
    [Intercept(typeof(AttributeRecordingInterceptor))]
    private sealed class MethodDisableInterceptionController
    {
        /// <summary>验证 Echo 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>Echo 的执行结果</returns>
        [DisableInterception]
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    /// <summary>验证 ControllerDisableInterceptionController 相关行为</summary>
    [Intercept(typeof(AttributeRecordingInterceptor))]
    [DisableInterception]
    private sealed class ControllerDisableInterceptionController
    {
        /// <summary>验证 Echo 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>Echo 的执行结果</returns>
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    /// <summary>验证 NonInterceptor 相关行为</summary>
    private sealed class NonInterceptor;

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

    /// <summary>验证 ReplaceResultAfterProceedInterceptor 相关行为</summary>
    private sealed class ReplaceResultAfterProceedInterceptor : IInterceptor
    {
        /// <summary>表示 Result 字段</summary>
        public static readonly OkObjectResult Result = new("replacement");

        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            await context.ProceedAsync().ConfigureAwait(false);

            context.ReturnValue = Result;
        }
    }

    /// <summary>验证 ConvertExceptionToResultInterceptor 相关行为</summary>
    private sealed class ConvertExceptionToResultInterceptor : IInterceptor
    {
        /// <summary>表示 Result 字段</summary>
        public static readonly OkResult Result = new();

        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
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
