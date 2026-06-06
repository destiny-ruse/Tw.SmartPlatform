using FluentAssertions;
using Tw.Core.Tests.DynamicProxy.Fakes;
using Tw.DynamicProxy.Abstractions;
using Xunit;

namespace Tw.Core.Tests.DynamicProxy;

public class SyncInterceptorBaseTests
{
    private sealed class RecordingInterceptor : SyncInterceptorBase
    {
        public List<string> Calls { get; } = [];

        protected override void Before(IInvocationContext context) => Calls.Add("before");
        protected override void After(IInvocationContext context) => Calls.Add("after");
        protected override void OnException(IInvocationContext context, Exception exception) =>
            Calls.Add("onexception");
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
    }
}
