using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Tw.AspNetCore.Mvc.DynamicProxy;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.DynamicProxy;

public class MvcInvocationContextTests
{
    [Fact]
    public void Constructor_MaterializesArgumentsInActionParameterOrder()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["third"] = "c",
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);

        var context = new MvcInvocationContext(executingContext, () => CreateExecutedContext(actionContext, controller));

        context.Arguments.Should().Equal("a", 2, "c");
        context.ArgumentsByName.Should().ContainKey("first").WhoseValue.Should().Be("a");
        context.ArgumentsByName.Should().ContainKey("second").WhoseValue.Should().Be(2);
        context.ArgumentsByName.Should().ContainKey("third").WhoseValue.Should().Be("c");
    }

    [Fact]
    public async Task ProceedAsync_WritesModifiedArgumentsBackBeforeCallingNext()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["third"] = "c",
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);
        object? firstValueSeenByNext = null;
        object? secondValueSeenByNext = null;

        var context = new MvcInvocationContext(
            executingContext,
            () =>
            {
                firstValueSeenByNext = actionArguments["first"];
                secondValueSeenByNext = actionArguments["second"];

                return CreateExecutedContext(actionContext, controller);
            });

        context.Arguments[0] = "rewritten";
        context.Arguments[1] = 42;
        await context.ProceedAsync();

        firstValueSeenByNext.Should().Be("rewritten");
        secondValueSeenByNext.Should().Be(42);
        actionArguments["first"].Should().Be("rewritten");
        actionArguments["second"].Should().Be(42);
    }

    [Fact]
    public void ArgumentsByName_IsReadOnlySnapshot()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["third"] = "c",
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);

        var context = new MvcInvocationContext(executingContext, () => CreateExecutedContext(actionContext, controller));

        context.ArgumentsByName.Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>();
        context.Arguments[0] = "rewritten";
        context.ArgumentsByName["first"].Should().Be("a");

        if (context.ArgumentsByName is IDictionary<string, object?> mutableDictionary)
        {
            var act = () => mutableDictionary["first"] = "mutated";

            act.Should().Throw<NotSupportedException>();
        }
    }

    [Fact]
    public void Constructor_UsesControllerActionDescriptorMethodInfo()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["third"] = "c",
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);
        var expectedMethod = typeof(SampleController).GetMethod(nameof(SampleController.MixedOrder))!;

        var context = new MvcInvocationContext(executingContext, () => CreateExecutedContext(actionContext, controller));

        context.Method.Should().BeSameAs(expectedMethod);
        context.Target.Should().BeSameAs(controller);
    }

    [Fact]
    public async Task ProceedAsync_CapturesActionExecutedResultAsReturnValue()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["third"] = "c",
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);
        var expectedResult = new OkObjectResult("result");
        var context = new MvcInvocationContext(
            executingContext,
            () => CreateExecutedContext(actionContext, controller, expectedResult));

        await context.ProceedAsync();

        context.ReturnValue.Should().BeSameAs(expectedResult);
        context.ReturnValue = new BadRequestResult();
        context.ReturnValue.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task ProceedAsync_RethrowsUnhandledActionExecutedException()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["third"] = "c",
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);
        var expectedException = new InvalidOperationException("action failed");
        var context = new MvcInvocationContext(
            executingContext,
            () => CreateExecutedContext(
                actionContext,
                controller,
                exception: expectedException,
                exceptionHandled: false));

        var act = async () => await context.ProceedAsync();

        var assertion = await act.Should().ThrowAsync<InvalidOperationException>();
        assertion.Which.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task ReturnValueSetter_MarksActionExecutedExceptionHandled_WhenExceptionIsConvertedToActionResult()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["third"] = "c",
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);
        var expectedException = new InvalidOperationException("action failed");
        var executedContext = new ActionExecutedContext(actionContext, [], controller)
        {
            Exception = expectedException,
            ExceptionHandled = false,
        };
        var context = new MvcInvocationContext(
            executingContext,
            () => Task.FromResult(executedContext));

        var act = async () => await context.ProceedAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        var result = new OkResult();
        context.ReturnValue = result;

        executedContext.Result.Should().BeSameAs(result);
        executedContext.ExceptionHandled.Should().BeTrue();
    }

    [Fact]
    public async Task ProceedAsync_DoesNotThrowHandledActionExecutedExceptionAndCapturesResult()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["third"] = "c",
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);
        var expectedException = new InvalidOperationException("handled failure");
        var expectedResult = new OkResult();
        var context = new MvcInvocationContext(
            executingContext,
            () => CreateExecutedContext(
                actionContext,
                controller,
                expectedResult,
                expectedException,
                exceptionHandled: true));

        await context.ProceedAsync();

        context.ReturnValue.Should().BeSameAs(expectedResult);
        executingContext.Result.Should().BeSameAs(expectedResult);
    }

    [Fact]
    public void ReturnValueSetter_WritesActionResultToActionExecutingContextResult()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["third"] = "c",
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);
        var expectedResult = new OkResult();
        var nextCalled = false;
        var context = new MvcInvocationContext(
            executingContext,
            () =>
            {
                nextCalled = true;

                return CreateExecutedContext(actionContext, controller);
            });

        context.ReturnValue = expectedResult;

        context.ReturnValue.Should().BeSameAs(expectedResult);
        executingContext.Result.Should().BeSameAs(expectedResult);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ReturnValueSetter_UpdatesActionExecutedContextResultAfterProceedAsync()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["third"] = "c",
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);
        var originalResult = new OkResult();
        var replacementResult = new BadRequestResult();
        var executedContext = new ActionExecutedContext(actionContext, [], controller)
        {
            Result = originalResult,
        };
        var context = new MvcInvocationContext(
            executingContext,
            () => Task.FromResult(executedContext));

        await context.ProceedAsync();
        context.ReturnValue = replacementResult;

        context.ReturnValue.Should().BeSameAs(replacementResult);
        executedContext.Result.Should().BeSameAs(replacementResult);
        executingContext.Result.Should().BeSameAs(replacementResult);
    }

    [Fact]
    public void Proceed_ThrowsInvalidOperationException_ForMvcAsyncFilterContext()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["third"] = "c",
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);
        var context = new MvcInvocationContext(executingContext, () => CreateExecutedContext(actionContext, controller));

        var act = context.Proceed;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProceedAsync*");
    }

    [Fact]
    public void Constructor_ThrowsInvalidOperationException_WhenArgumentMappingIsMissing()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);

        var act = () => new MvcInvocationContext(executingContext, () => CreateExecutedContext(actionContext, controller));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MixedOrder*third*");
    }

    [Fact]
    public async Task ProceedAsync_ThrowsInvalidOperationException_WhenArgumentMappingIsRemoved()
    {
        var controller = new SampleController();
        var actionContext = CreateActionContext(nameof(SampleController.MixedOrder));
        var actionArguments = new Dictionary<string, object?>
        {
            ["third"] = "c",
            ["first"] = "a",
            ["second"] = 2,
        };
        var executingContext = CreateExecutingContext(actionContext, actionArguments, controller);
        var context = new MvcInvocationContext(executingContext, () => CreateExecutedContext(actionContext, controller));
        actionArguments.Remove("second");

        var act = async () => await context.ProceedAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*MixedOrder*second*");
    }

    [Fact]
    public void Constructor_ThrowsInvalidOperationException_WhenActionDescriptorIsNotControllerActionDescriptor()
    {
        var controller = new SampleController();
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor { DisplayName = "PlainAction" });
        var executingContext = CreateExecutingContext(actionContext, new Dictionary<string, object?>(), controller);

        var act = () => new MvcInvocationContext(executingContext, () => CreateExecutedContext(actionContext, controller));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ControllerActionDescriptor*PlainAction*");
    }

    private static ActionContext CreateActionContext(string actionName)
    {
        var method = typeof(SampleController).GetMethod(actionName)!;
        var actionDescriptor = new ControllerActionDescriptor
        {
            ActionName = actionName,
            ControllerName = nameof(SampleController),
            ControllerTypeInfo = typeof(SampleController).GetTypeInfo(),
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
        IActionResult? result = null,
        Exception? exception = null,
        bool exceptionHandled = false)
    {
        return Task.FromResult(new ActionExecutedContext(actionContext, [], controller)
        {
            Exception = exception,
            ExceptionHandled = exceptionHandled,
            Result = result,
        });
    }

    private sealed class SampleController
    {
        public IActionResult MixedOrder(string first, int second, string third) => new OkResult();
    }
}
