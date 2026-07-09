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

/// <summary>验证 PageInvocationContextTests 相关行为</summary>
public class PageInvocationContextTests
{
    /// <summary>验证 SamplePageModel 相关行为</summary>
    private sealed class SamplePageModel : PageModel
    {
        /// <summary>验证 OnGet 场景</summary>
        /// <param name="value">value 参数</param>
        /// <param name="count">count 参数</param>
        /// <returns>OnGet 的执行结果</returns>
        public IActionResult OnGet(string value, int count) => new OkObjectResult($"{value}:{count}");
    }

    /// <summary>验证 CreatePageContext 场景</summary>
    /// <returns>CreatePageContext 的执行结果</returns>
    private static PageContext CreatePageContext()
    {
        var actionDescriptor = new CompiledPageActionDescriptor
        {
            DisplayName = "/Sample",
        };

        return new PageContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor));
    }

    /// <summary>验证 CreateHandlerMethod 场景</summary>
    /// <param name="name">name 参数</param>
    /// <returns>CreateHandlerMethod 的执行结果</returns>
    private static HandlerMethodDescriptor CreateHandlerMethod(string name = nameof(SamplePageModel.OnGet)) =>
        new()
        {
            MethodInfo = typeof(SamplePageModel).GetMethod(name)!,
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
            handlerMethod ?? CreateHandlerMethod(),
            handlerArguments,
            handlerInstance);

    /// <summary>验证 CreateNext 场景</summary>
    /// <param name="executingContext">executingContext 参数</param>
    /// <param name="result">result 参数</param>
    /// <param name="onNext">onNext 参数</param>
    /// <returns>CreateNext 的执行结果</returns>
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

    /// <summary>验证 Constructor_MapsMethodTargetArgumentsAndNamedView 场景</summary>
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

    /// <summary>验证 Constructor_Throws_WhenHandlerArgumentMissing 场景</summary>
    [Fact]
    public void Constructor_Throws_WhenHandlerArgumentMissing()
    {
        var executingContext = CreateExecutingContext(
            new Dictionary<string, object?> { ["value"] = "a" },
            new SamplePageModel());

        var act = () => new PageInvocationContext(executingContext, CreateNext(executingContext));

        act.Should().Throw<InvalidOperationException>().WithMessage("*count*");
    }

    /// <summary>验证 ProceedAsync_WritesModifiedArgumentsBackToHandlerArguments 场景</summary>
    /// <returns>ProceedAsync_WritesModifiedArgumentsBackToHandlerArguments 的执行结果</returns>
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

    /// <summary>验证 ProceedAsync_CapturesHandlerResultAsReturnValue 场景</summary>
    /// <returns>ProceedAsync_CapturesHandlerResultAsReturnValue 的执行结果</returns>
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

    /// <summary>验证 ProceedAsync_Rethrows_WhenHandlerThrewUnhandledException 场景</summary>
    /// <returns>ProceedAsync_Rethrows_WhenHandlerThrewUnhandledException 的执行结果</returns>
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

    /// <summary>验证 ReturnValue_SetBeforeProceed_ShortCircuitsExecutingContextResult 场景</summary>
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

    /// <summary>验证 Proceed_Throws_BecausePageHandlerFilterIsAsyncOnly 场景</summary>
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
