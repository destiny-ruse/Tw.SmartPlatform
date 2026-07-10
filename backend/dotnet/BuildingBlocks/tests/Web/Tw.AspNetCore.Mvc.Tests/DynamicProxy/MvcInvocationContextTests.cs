using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Tw.AspNetCore.Mvc.DynamicProxy;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.DynamicProxy;

/// <summary>
/// 覆盖MVC调用上下文的核心行为和边界条件
/// </summary>
public class MvcInvocationContextTests
{
    /// <summary>
    /// 验证构造函数Materializes参数InActionParameterOrder
    /// </summary>
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

    /// <summary>
    /// 验证继续处理异步写回已修改参数回前置处理CallingNext
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证参数By名称IsReadOnlySnapshot
    /// </summary>
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

    /// <summary>
    /// 验证构造函数Uses控制器ActionDescriptor方法Info
    /// </summary>
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

    /// <summary>
    /// 验证继续处理异步捕获ActionExecuted结果作为返回值
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证继续处理异步重新抛出未处理ActionExecuted异常
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证返回值SetterMarksActionExecuted异常Handled当异常IsConverted到Action结果
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证继续处理异步不ThrowHandledActionExecuted异常和捕获结果
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证返回值Setter写回Action结果到ActionExecuting上下文结果
    /// </summary>
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

    /// <summary>
    /// 验证返回值SetterUpdatesActionExecuted上下文结果After继续处理异步
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证继续处理抛出异常非法业务委托异常针对MVC异步过滤器上下文
    /// </summary>
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

    /// <summary>
    /// 验证构造函数抛出异常非法业务委托异常当参数MappingIs缺少
    /// </summary>
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

    /// <summary>
    /// 验证继续处理异步抛出异常非法业务委托异常当参数MappingIsRemoved
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证构造函数抛出异常非法业务委托异常当ActionDescriptorIs不控制器ActionDescriptor
    /// </summary>
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

    /// <summary>
    /// 创建Action上下文测试对象
    /// </summary>
    /// <param name="actionName">目标 MVC Action 的名称</param>
    /// <returns>方法计算得到的文本值</returns>
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
    /// <param name="exception">用于模拟异常流程的异常实例</param>
    /// <param name="exceptionHandled">指示异常是否已被过滤器处理</param>
    /// <returns>异步流程完成后产生的ActionExecuted上下文</returns>
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

    /// <summary>
    /// 覆盖示例Controller的核心行为和边界条件
    /// </summary>
    private sealed class SampleController
    {
        /// <summary>
        /// 按混合顺序返回参数以验证名称绑定
        /// </summary>
        /// <param name="first">用于校验参数顺序的第一个值</param>
        /// <param name="second">用于校验参数顺序的第二个值</param>
        /// <param name="third">用于校验参数顺序的第三个值</param>
        /// <returns>MVC 或 Razor Page 处理结果</returns>
        public IActionResult MixedOrder(string first, int second, string third) => new OkResult();
    }
}
