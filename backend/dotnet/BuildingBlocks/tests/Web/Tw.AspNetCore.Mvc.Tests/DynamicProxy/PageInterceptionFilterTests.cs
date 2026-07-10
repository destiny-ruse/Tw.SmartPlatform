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

/// <summary>
/// 覆盖页面Interception过滤器的核心行为和边界条件
/// </summary>
public class PageInterceptionFilterTests
{
    /// <summary>
    /// 验证On页面处理器Execution异步带有已选择拦截器Invokes管道和写回已修改参数到处理器
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证On页面处理器Execution异步不带已选择拦截器CallsNext和不调用管道
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证On页面处理器Execution异步当处理器方法缺少CallsNext不带Selection
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证On页面处理器Execution异步当拦截器短路流程SetsExecuting结果和不CallNext
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证On页面处理器Execution异步带有特性Selector和处理器Intercept特性Invokes拦截器
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证添加MVCIntegration注册页面Interception过滤器
    /// </summary>
    [Fact]
    public void AddMvcIntegration_RegistersPageInterceptionFilter()
    {
        var services = new ServiceCollection();

        services.AddMvcIntegration();

        using var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        mvcOptions.Filters.Count(IsPageInterceptionFilter).Should().Be(1);
    }

    /// <summary>
    /// 验证添加MVCIntegration当调用两次注册页面Interception过滤器一次
    /// </summary>
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
        services.AddTransient<TwPageInterceptionFilter>();

        return services;
    }

    /// <summary>
    /// 创建页面上下文测试对象
    /// </summary>
    /// <returns>Razor Page 测试上下文</returns>
    private static PageContext CreatePageContext() =>
        new(new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new CompiledPageActionDescriptor { DisplayName = "/Sample" }));

    /// <summary>
    /// 创建处理器方法测试对象
    /// </summary>
    /// <typeparam name="TPageModel">响应数据的运行时类型</typeparam>
    /// <param name="name">待匹配成员或资源的名称</param>
    /// <returns>页面处理器方法描述符</returns>
    private static HandlerMethodDescriptor CreateHandlerMethod<TPageModel>(string name) =>
        new()
        {
            MethodInfo = typeof(TPageModel).GetMethod(name)!,
            Name = name,
        };

    /// <summary>
    /// 创建Executing上下文测试对象
    /// </summary>
    /// <param name="handlerArguments">页面处理器调用时使用的参数字典</param>
    /// <param name="handlerInstance">页面处理器所属的页面模型实例</param>
    /// <param name="handlerMethod">当前页面处理器的方法元数据</param>
    /// <returns>页面处理器执行阶段的测试上下文</returns>
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

    /// <summary>
    /// 创建Executed上下文测试对象
    /// </summary>
    /// <param name="executingContext">页面处理器执行阶段的上下文</param>
    /// <param name="result">当前流程预置或返回的结果</param>
    /// <returns>异步流程完成后产生的页面处理器Executed上下文</returns>
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

    /// <summary>
    /// 判断页面Interception过滤器是否满足条件
    /// </summary>
    /// <param name="filter">参与测试的 MVC 或页面过滤器实例</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsPageInterceptionFilter(IFilterMetadata filter) =>
        filter is TypeFilterAttribute typeFilter
        && typeFilter.ImplementationType == typeof(TwPageInterceptionFilter);

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
    /// 覆盖Throwing拦截器Selector的核心行为和边界条件
    /// </summary>
    private sealed class ThrowingInterceptorSelector : IInterceptorSelector
    {
        /// <summary>
        /// 按服务类型和方法选择拦截器类型
        /// </summary>
        /// <param name="implementationType">服务注册中使用的实现类型</param>
        /// <param name="serviceType">服务注册中暴露的服务类型</param>
        /// <param name="method">用于构造测试场景的方法元数据</param>
        /// <returns>匹配当前查询条件的结果集合</returns>
        public IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, MethodInfo method) =>
            throw new InvalidOperationException("selector should not run when handler method is missing");
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
    /// 覆盖示例页面模型的核心行为和边界条件
    /// </summary>
    private sealed class SamplePageModel : PageModel
    {
        /// <summary>
        /// 处理页面 GET 请求并返回响应结果
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>MVC 或 Razor Page 处理结果</returns>
        public IActionResult OnGet(string value) => new OkObjectResult(value);
    }

    /// <summary>
    /// 覆盖Intercepted页面模型的核心行为和边界条件
    /// </summary>
    private sealed class InterceptedPageModel : PageModel
    {
        /// <summary>
        /// 处理页面 GET 请求并返回响应结果
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>MVC 或 Razor Page 处理结果</returns>
        [Intercept(typeof(AttributeRecordingInterceptor))]
        public IActionResult OnGet(string value) => new OkObjectResult(value);
    }

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
}
