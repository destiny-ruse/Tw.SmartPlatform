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

/// <summary>
/// 覆盖MVCInterception过滤器的核心行为和边界条件
/// </summary>
public class MvcInterceptionFilterTests
{
    /// <summary>
    /// 验证OnActionExecution异步带有已选择拦截器Invokes管道和写回已修改参数到Action
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证OnActionExecution异步不带已选择拦截器CallsNext和不调用管道
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证OnActionExecution异步不带已选择拦截器不Resolve管道
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证OnActionExecution异步当拦截器短路流程SetsExecuting结果和不CallNext
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证OnActionExecution异步当拦截器Replaces结果After继续处理UpdatesExecuted上下文结果
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证OnActionExecution异步当拦截器Converts异常到结果Marks异常Handled
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证OnActionExecution异步带有特性Selector和方法Intercept特性Invokes拦截器
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证OnActionExecution异步带有特性Selector和控制器Intercept特性Invokes拦截器
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证OnActionExecution异步带有特性Selector和方法DisableInterception不调用拦截器
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证OnActionExecution异步带有特性Selector和控制器DisableInterception不调用拦截器
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证OnActionExecution异步当已选择类型不Implement拦截器抛出异常Clear非法业务委托异常
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证添加MVCIntegration返回SameServices和注册MVC过滤器和Cancellation令牌提供器
    /// </summary>
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

    /// <summary>
    /// 验证添加MVCIntegration当调用两次注册ActionInterception过滤器一次
    /// </summary>
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

    /// <summary>
    /// 创建Services测试对象
    /// </summary>
    /// <param name="interceptorTypes">需要注册或选择的拦截器类型集合</param>
    /// <returns>匹配当前查询条件的结果集合</returns>
    private static ServiceCollection CreateServices(IReadOnlyList<Type> interceptorTypes)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorSelector>(new FixedInterceptorSelector(interceptorTypes));
        services.AddSingleton<IInterceptorPipeline, InterceptorPipeline>();
        services.AddTransient<TwActionInterceptionFilter>();

        return services;
    }

    /// <summary>
    /// 创建特性SelectorServices测试对象
    /// </summary>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static ServiceCollection CreateAttributeSelectorServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IInterceptorSelector, AttributeInterceptorSelector>();
        services.AddSingleton<IInterceptorPipeline, InterceptorPipeline>();
        services.AddTransient<TwActionInterceptionFilter>();

        return services;
    }

    /// <summary>
    /// 创建Action上下文测试对象
    /// </summary>
    /// <param name="actionName">目标 MVC Action 的名称</param>
    /// <returns>方法计算得到的文本值</returns>
    private static ActionContext CreateActionContext(string actionName) =>
        CreateActionContext<SampleController>(actionName);

    /// <summary>
    /// 创建Action上下文测试对象
    /// </summary>
    /// <typeparam name="TController">响应数据的运行时类型</typeparam>
    /// <param name="actionName">目标 MVC Action 的名称</param>
    /// <returns>方法计算得到的文本值</returns>
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

    /// <summary>
    /// 创建Executing上下文测试对象
    /// </summary>
    /// <param name="actionContext">MVC Action 执行所需的上下文</param>
    /// <param name="actionArguments">MVC Action 调用时使用的参数字典</param>
    /// <param name="controller">承载当前 Action 的控制器实例</param>
    /// <returns>页面处理器执行阶段的测试上下文</returns>
    private static ActionExecutingContext CreateExecutingContext(
        ActionContext actionContext,
        IDictionary<string, object?> actionArguments,
        object controller) =>
        new(actionContext, [], actionArguments, controller);

    /// <summary>
    /// 创建Executed上下文测试对象
    /// </summary>
    /// <param name="actionContext">MVC Action 执行所需的上下文</param>
    /// <param name="controller">承载当前 Action 的控制器实例</param>
    /// <param name="result">当前流程预置或返回的结果</param>
    /// <returns>异步流程完成后产生的ActionExecuted上下文</returns>
    private static Task<ActionExecutedContext> CreateExecutedContext(
        ActionContext actionContext,
        object controller,
        IActionResult? result = null) =>
        Task.FromResult(new ActionExecutedContext(actionContext, [], controller)
        {
            Result = result,
        });

    /// <summary>
    /// 判断ActionInterception过滤器是否满足条件
    /// </summary>
    /// <param name="filter">参与测试的 MVC 或页面过滤器实例</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsActionInterceptionFilter(IFilterMetadata filter) =>
        filter is TypeFilterAttribute typeFilter
        && typeFilter.ImplementationType == typeof(TwActionInterceptionFilter);

    /// <summary>
    /// 覆盖Fixed拦截器Selector的核心行为和边界条件
    /// </summary>
    private sealed class FixedInterceptorSelector(IReadOnlyList<Type> interceptorTypes) : IInterceptorSelector
    {
        /// <summary>
        /// 按服务类型和方法选择拦截器类型
        /// </summary>
        /// <param name="implementationType">服务注册中使用的实现类型</param>
        /// <param name="serviceType">服务注册中暴露的服务类型</param>
        /// <param name="method">用于构造测试场景的方法元数据</param>
        /// <returns>匹配当前查询条件的结果集合</returns>
        public IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, MethodInfo method) =>
            interceptorTypes;
    }

    /// <summary>
    /// 覆盖Counting管道的核心行为和边界条件
    /// </summary>
    private sealed class CountingPipeline : IInterceptorPipeline
    {
        /// <summary>
        /// 测试替身记录的调用次数
        /// </summary>
        public int InvokeCount { get; private set; }

        /// <summary>
        /// 执行测试管道委托并记录调用
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <param name="interceptors">参与当前测试场景的拦截器集合</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InvokeAsync(IInvocationContext context, IReadOnlyList<IInterceptor> interceptors)
        {
            InvokeCount++;

            return context.ProceedAsync();
        }
    }

    /// <summary>
    /// 覆盖示例Controller的核心行为和边界条件
    /// </summary>
    private sealed class SampleController
    {
        /// <summary>
        /// 返回传入值以支持拦截断言
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>MVC 或 Razor Page 处理结果</returns>
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    /// <summary>
    /// 覆盖MethodInterceptController的核心行为和边界条件
    /// </summary>
    private sealed class MethodInterceptController
    {
        /// <summary>
        /// 返回传入值以支持拦截断言
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>MVC 或 Razor Page 处理结果</returns>
        [Intercept(typeof(AttributeRecordingInterceptor))]
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    /// <summary>
    /// 覆盖ControllerInterceptController的核心行为和边界条件
    /// </summary>
    [Intercept(typeof(AttributeRecordingInterceptor))]
    private sealed class ControllerInterceptController
    {
        /// <summary>
        /// 返回传入值以支持拦截断言
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>MVC 或 Razor Page 处理结果</returns>
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    /// <summary>
    /// 覆盖MethodDisableInterceptionController的核心行为和边界条件
    /// </summary>
    [Intercept(typeof(AttributeRecordingInterceptor))]
    private sealed class MethodDisableInterceptionController
    {
        /// <summary>
        /// 返回传入值以支持拦截断言
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>MVC 或 Razor Page 处理结果</returns>
        [DisableInterception]
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    /// <summary>
    /// 覆盖ControllerDisableInterceptionController的核心行为和边界条件
    /// </summary>
    [Intercept(typeof(AttributeRecordingInterceptor))]
    [DisableInterception]
    private sealed class ControllerDisableInterceptionController
    {
        /// <summary>
        /// 返回传入值以支持拦截断言
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>MVC 或 Razor Page 处理结果</returns>
        public IActionResult Echo(string value) => new OkObjectResult(value);
    }

    /// <summary>
    /// 覆盖Non拦截器的核心行为和边界条件
    /// </summary>
    private sealed class NonInterceptor;

    /// <summary>
    /// 覆盖RewriteArgument拦截器的核心行为和边界条件
    /// </summary>
    private sealed class RewriteArgumentInterceptor : IInterceptor
    {
        /// <summary>
        /// 测试替身是否已经被调用
        /// </summary>
        public static bool WasCalled { get; set; }

        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            WasCalled = true;
            context.Arguments[0] = "rewritten";

            await context.ProceedAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 覆盖特性Recording拦截器的核心行为和边界条件
    /// </summary>
    private sealed class AttributeRecordingInterceptor : IInterceptor
    {
        /// <summary>
        /// 测试替身记录的调用次数
        /// </summary>
        public static int CallCount { get; set; }

        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            CallCount++;

            await context.ProceedAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 覆盖短路Circuit拦截器的核心行为和边界条件
    /// </summary>
    private sealed class ShortCircuitInterceptor : IInterceptor
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的结果
        /// </summary>
        public static readonly OkResult Result = new();

        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context)
        {
            context.ReturnValue = Result;

            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// 覆盖Replace结果After继续处理拦截器的核心行为和边界条件
    /// </summary>
    private sealed class ReplaceResultAfterProceedInterceptor : IInterceptor
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的结果
        /// </summary>
        public static readonly OkObjectResult Result = new("replacement");

        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            await context.ProceedAsync().ConfigureAwait(false);

            context.ReturnValue = Result;
        }
    }

    /// <summary>
    /// 覆盖ConvertExceptionTo结果拦截器的核心行为和边界条件
    /// </summary>
    private sealed class ConvertExceptionToResultInterceptor : IInterceptor
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的结果
        /// </summary>
        public static readonly OkResult Result = new();

        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
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
