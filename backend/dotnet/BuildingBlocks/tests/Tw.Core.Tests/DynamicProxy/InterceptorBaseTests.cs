using AwesomeAssertions;
using Tw.Core.Tests.DynamicProxy.Fakes;
using Tw.DynamicProxy.Abstractions;
using Xunit;

namespace Tw.Core.Tests.DynamicProxy;

public class InterceptorBaseTests
{
    private sealed class RecordingInterceptor : InterceptorBase
    {
        public List<string> Calls { get; } = [];

        protected override ValueTask BeforeAsync(IInvocationContext context)
        {
            Calls.Add("before");
            return ValueTask.CompletedTask;
        }

        protected override ValueTask AfterAsync(IInvocationContext context)
        {
            Calls.Add("after");
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnExceptionAsync(IInvocationContext context, Exception exception)
        {
            Calls.Add("onexception");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingBeforeInterceptor : InterceptorBase
    {
        public List<string> Calls { get; } = [];

        protected override ValueTask BeforeAsync(IInvocationContext context) =>
            throw new InvalidOperationException("before-boom");

        protected override ValueTask AfterAsync(IInvocationContext context)
        {
            Calls.Add("after");
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnExceptionAsync(IInvocationContext context, Exception exception)
        {
            Calls.Add("onexception");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task HappyPath_RunsBeforeProceedAfter_WithoutOnException()
    {
        var sut = new RecordingInterceptor();
        var context = new FakeInvocationContext();

        await sut.InterceptAsync(context);

        sut.Calls.Should().Equal("before", "after");
        context.ProceedCount.Should().Be(1);
    }

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
