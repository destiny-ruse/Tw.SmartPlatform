using AwesomeAssertions;
using Tw.Castle.Core.Tests.Abstractions.Fakes;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests.Abstractions;

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

    private sealed class ThrowingBeforeInterceptor : SyncInterceptorBase
    {
        public List<string> Calls { get; } = [];

        protected override void Before(IInvocationContext context) =>
            throw new InvalidOperationException("before-boom");
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
