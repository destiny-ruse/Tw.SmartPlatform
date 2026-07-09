using AwesomeAssertions;
using Tw.Castle.Core.Tests.Abstractions.Fakes;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests.Abstractions;

/// <summary>验证 InterceptorBaseTests 相关行为</summary>
public class InterceptorBaseTests
{
    /// <summary>验证 RecordingInterceptor 相关行为</summary>
    private sealed class RecordingInterceptor : InterceptorBase
    {
        /// <summary>表示 Calls 属性</summary>
        public List<string> Calls { get; } = [];

        /// <summary>验证 BeforeAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>BeforeAsync 的执行结果</returns>
        protected override ValueTask BeforeAsync(IInvocationContext context)
        {
            Calls.Add("before");
            return ValueTask.CompletedTask;
        }

        /// <summary>验证 AfterAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>AfterAsync 的执行结果</returns>
        protected override ValueTask AfterAsync(IInvocationContext context)
        {
            Calls.Add("after");
            return ValueTask.CompletedTask;
        }

        /// <summary>验证 OnExceptionAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <param name="exception">exception 参数</param>
        /// <returns>OnExceptionAsync 的执行结果</returns>
        protected override ValueTask OnExceptionAsync(IInvocationContext context, Exception exception)
        {
            Calls.Add("onexception");
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>验证 ThrowingBeforeInterceptor 相关行为</summary>
    private sealed class ThrowingBeforeInterceptor : InterceptorBase
    {
        /// <summary>表示 Calls 属性</summary>
        public List<string> Calls { get; } = [];

        /// <summary>验证 BeforeAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>BeforeAsync 的执行结果</returns>
        protected override ValueTask BeforeAsync(IInvocationContext context) =>
            throw new InvalidOperationException("before-boom");

        /// <summary>验证 AfterAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>AfterAsync 的执行结果</returns>
        protected override ValueTask AfterAsync(IInvocationContext context)
        {
            Calls.Add("after");
            return ValueTask.CompletedTask;
        }

        /// <summary>验证 OnExceptionAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <param name="exception">exception 参数</param>
        /// <returns>OnExceptionAsync 的执行结果</returns>
        protected override ValueTask OnExceptionAsync(IInvocationContext context, Exception exception)
        {
            Calls.Add("onexception");
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>验证 HappyPath_RunsBeforeProceedAfter_WithoutOnException 场景</summary>
    /// <returns>HappyPath_RunsBeforeProceedAfter_WithoutOnException 的执行结果</returns>
    [Fact]
    public async Task HappyPath_RunsBeforeProceedAfter_WithoutOnException()
    {
        var sut = new RecordingInterceptor();
        var context = new FakeInvocationContext();

        await sut.InterceptAsync(context);

        sut.Calls.Should().Equal("before", "after");
        context.ProceedCount.Should().Be(1);
    }

    /// <summary>验证 ExceptionPath_RunsOnExceptionThenAfter_AndRethrows 场景</summary>
    /// <returns>ExceptionPath_RunsOnExceptionThenAfter_AndRethrows 的执行结果</returns>
    [Fact]
    public async Task ExceptionPath_RunsOnExceptionThenAfter_AndRethrows()
    {
        var sut = new RecordingInterceptor();
        var context = new FakeInvocationContext(
            () => throw new InvalidOperationException("boom"));

        var act = async () => await sut.InterceptAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        sut.Calls.Should().Equal("before", "onexception", "after");
        context.ProceedCount.Should().Be(1);
    }

    /// <summary>验证 BeforeThrows_DoesNotProceedOrRunAfterOrOnException_AndPropagates 场景</summary>
    /// <returns>BeforeThrows_DoesNotProceedOrRunAfterOrOnException_AndPropagates 的执行结果</returns>
    [Fact]
    public async Task BeforeThrows_DoesNotProceedOrRunAfterOrOnException_AndPropagates()
    {
        var sut = new ThrowingBeforeInterceptor();
        var context = new FakeInvocationContext();

        var act = async () => await sut.InterceptAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("before-boom");
        context.ProceedCount.Should().Be(0);
        sut.Calls.Should().BeEmpty();
    }
}
