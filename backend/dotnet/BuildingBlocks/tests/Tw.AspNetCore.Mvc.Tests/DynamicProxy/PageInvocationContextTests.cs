using System.Reflection;
using FluentAssertions;
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

public class PageInvocationContextTests
{
    private sealed class SamplePageModel : PageModel
    {
        public IActionResult OnGet(string value, int count) => new OkObjectResult($"{value}:{count}");
    }

    private static PageContext CreatePageContext()
    {
        var actionDescriptor = new CompiledPageActionDescriptor
        {
            DisplayName = "/Sample",
        };

        return new PageContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor));
    }

    private static HandlerMethodDescriptor CreateHandlerMethod(string name = nameof(SamplePageModel.OnGet)) =>
        new()
        {
            MethodInfo = typeof(SamplePageModel).GetMethod(name)!,
            Name = name,
        };

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

    [Fact]
    public void Constructor_Throws_WhenHandlerArgumentMissing()
    {
        var executingContext = CreateExecutingContext(
            new Dictionary<string, object?> { ["value"] = "a" },
            new SamplePageModel());

        var act = () => new PageInvocationContext(executingContext, CreateNext(executingContext));

        act.Should().Throw<InvalidOperationException>().WithMessage("*count*");
    }

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
