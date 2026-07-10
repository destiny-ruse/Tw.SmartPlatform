using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Tw.AspNetCore.Mvc.DynamicProxy;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.DynamicProxy;

/// <summary>
/// 覆盖页面调用上下文的核心行为和边界条件
/// </summary>
public class PageInvocationContextTests
{
    /// <summary>
    /// 覆盖示例页面模型的核心行为和边界条件
    /// </summary>
    private sealed class SamplePageModel : PageModel
    {
        /// <summary>
        /// 处理页面 GET 请求并返回响应结果
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <param name="count">用于构造测试输入或断言的数量</param>
        /// <returns>MVC 或 Razor Page 处理结果</returns>
        public IActionResult OnGet(string value, int count) => new OkObjectResult($"{value}:{count}");
    }

    /// <summary>
    /// 创建页面上下文测试对象
    /// </summary>
    /// <returns>Razor Page 测试上下文</returns>
    private static PageContext CreatePageContext()
    {
        var actionDescriptor = new CompiledPageActionDescriptor
        {
            DisplayName = "/Sample",
        };

        return new PageContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor));
    }

    /// <summary>
    /// 创建处理器方法测试对象
    /// </summary>
    /// <param name="name">待匹配成员或资源的名称</param>
    /// <returns>页面处理器方法描述符</returns>
    private static HandlerMethodDescriptor CreateHandlerMethod(string name = nameof(SamplePageModel.OnGet)) =>
        new()
        {
            MethodInfo = typeof(SamplePageModel).GetMethod(name)!,
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
            handlerMethod ?? CreateHandlerMethod(),
            handlerArguments,
            handlerInstance);

    /// <summary>
    /// 创建Next测试对象
    /// </summary>
    /// <param name="executingContext">页面处理器执行阶段的上下文</param>
    /// <param name="result">当前流程预置或返回的结果</param>
    /// <param name="onNext">用于模拟后续过滤器或处理器的委托</param>
    /// <returns>模拟 ASP.NET Core 过滤器管道下一步的委托</returns>
    private static PageHandlerExecutionDelegate CreateNext(
        PageHandlerExecutingContext executingContext,
        IActionResult? result = null,
        Action? onNext = null) =>
        () =>
        {
            onNext?.Invoke();

            return Task.FromResult(new PageHandlerExecutedContext(
                new PageContext(new ActionContext(
                    executingContext.HttpContext,
                    executingContext.RouteData,
                    executingContext.ActionDescriptor)),
                [],
                executingContext.HandlerMethod!,
                executingContext.HandlerInstance)
            {
                Result = result,
            });
        };

    /// <summary>
    /// 验证构造函数映射方法目标参数和命名视图
    /// </summary>
    [Fact]
    public void Constructor_MapsMethodTargetArgumentsAndNamedView()
    {
        var model = new SamplePageModel();
        var executingContext = CreateExecutingContext(
            new Dictionary<string, object?> { ["value"] = "a", ["count"] = 2 },
            model);

        var context = new PageInvocationContext(executingContext, CreateNext(executingContext));

        context.Method.Should().BeSameAs(typeof(SamplePageModel).GetMethod(nameof(SamplePageModel.OnGet)));
        context.Target.Should().BeSameAs(model);
        context.Arguments.Should().Equal("a", 2);
        context.ArgumentsByName["value"].Should().Be("a");
        context.ArgumentsByName["count"].Should().Be(2);
    }

    /// <summary>
    /// 验证构造函数抛出异常当处理器参数缺少
    /// </summary>
    [Fact]
    public void Constructor_Throws_WhenHandlerArgumentMissing()
    {
        var executingContext = CreateExecutingContext(
            new Dictionary<string, object?> { ["value"] = "a" },
            new SamplePageModel());

        var act = () => new PageInvocationContext(executingContext, CreateNext(executingContext));

        act.Should().Throw<InvalidOperationException>().WithMessage("*count*");
    }

    /// <summary>
    /// 验证继续处理异步写回已修改参数回到处理器参数
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task ProceedAsync_WritesModifiedArgumentsBackToHandlerArguments()
    {
        var handlerArguments = new Dictionary<string, object?> { ["value"] = "a", ["count"] = 2 };
        var executingContext = CreateExecutingContext(handlerArguments, new SamplePageModel());
        object? valueSeenByNext = null;
        var context = new PageInvocationContext(
            executingContext,
            CreateNext(executingContext, onNext: () => valueSeenByNext = handlerArguments["value"]));
        context.Arguments[0] = "rewritten";

        await context.ProceedAsync();

        valueSeenByNext.Should().Be("rewritten");
        handlerArguments["value"].Should().Be("rewritten");
    }

    /// <summary>
    /// 验证继续处理异步捕获处理器结果作为返回值
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task ProceedAsync_CapturesHandlerResultAsReturnValue()
    {
        var executingContext = CreateExecutingContext(
            new Dictionary<string, object?> { ["value"] = "a", ["count"] = 2 },
            new SamplePageModel());
        var result = new OkObjectResult("done");
        var context = new PageInvocationContext(executingContext, CreateNext(executingContext, result));

        await context.ProceedAsync();

        context.ReturnValue.Should().BeSameAs(result);
    }

    /// <summary>
    /// 验证继续处理异步重新抛出当处理器抛出未处理异常
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task ProceedAsync_Rethrows_WhenHandlerThrewUnhandledException()
    {
        var executingContext = CreateExecutingContext(
            new Dictionary<string, object?> { ["value"] = "a", ["count"] = 2 },
            new SamplePageModel());
        PageHandlerExecutionDelegate next = () => Task.FromResult(new PageHandlerExecutedContext(
            CreatePageContext(),
            [],
            executingContext.HandlerMethod!,
            executingContext.HandlerInstance)
        {
            Exception = new InvalidOperationException("handler failed"),
            ExceptionHandled = false,
        });
        var context = new PageInvocationContext(executingContext, next);

        var act = async () => await context.ProceedAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler failed");
    }

    /// <summary>
    /// 验证返回值写入前置处理继续处理短路流程Executing上下文结果
    /// </summary>
    [Fact]
    public void ReturnValue_SetBeforeProceed_ShortCircuitsExecutingContextResult()
    {
        var executingContext = CreateExecutingContext(
            new Dictionary<string, object?> { ["value"] = "a", ["count"] = 2 },
            new SamplePageModel());
        var context = new PageInvocationContext(executingContext, CreateNext(executingContext));
        var result = new OkResult();

        context.ReturnValue = result;

        executingContext.Result.Should().BeSameAs(result);
    }

    /// <summary>
    /// 验证继续处理抛出异常Because页面处理器过滤器Is异步Only
    /// </summary>
    [Fact]
    public void Proceed_Throws_BecausePageHandlerFilterIsAsyncOnly()
    {
        var executingContext = CreateExecutingContext(
            new Dictionary<string, object?> { ["value"] = "a", ["count"] = 2 },
            new SamplePageModel());
        var context = new PageInvocationContext(executingContext, CreateNext(executingContext));

        var act = () => context.Proceed();

        act.Should().Throw<InvalidOperationException>().WithMessage("*ProceedAsync*");
    }
}
