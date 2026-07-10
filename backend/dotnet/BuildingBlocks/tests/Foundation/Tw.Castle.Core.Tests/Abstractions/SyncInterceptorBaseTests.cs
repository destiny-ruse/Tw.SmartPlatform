using AwesomeAssertions;
using Tw.Castle.Core.Tests.Abstractions.Fakes;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests.Abstractions;

/// <summary>
/// 覆盖Sync拦截器Base的核心行为和边界条件
/// </summary>
public class SyncInterceptorBaseTests
{
    /// <summary>
    /// 覆盖Recording拦截器的核心行为和边界条件
    /// </summary>
    private sealed class RecordingInterceptor : SyncInterceptorBase
    {
        /// <summary>
        /// Calls在当前对象中的业务含义
        /// </summary>
        public List<string> Calls { get; } = [];

        /// <summary>
        /// 在目标调用前运行拦截器逻辑
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        protected override void Before(IInvocationContext context) => Calls.Add("before");
        /// <summary>
        /// 说明After在当前类型中的职责
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        protected override void After(IInvocationContext context) => Calls.Add("after");
        /// <summary>
        /// 说明On异常在当前类型中的职责
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <param name="exception">用于模拟异常流程的异常实例</param>
        protected override void OnException(IInvocationContext context, Exception exception) =>
            Calls.Add("onexception");
    }

    /// <summary>
    /// 覆盖Throwing前置处理拦截器的核心行为和边界条件
    /// </summary>
    private sealed class ThrowingBeforeInterceptor : SyncInterceptorBase
    {
        /// <summary>
        /// Calls在当前对象中的业务含义
        /// </summary>
        public List<string> Calls { get; } = [];

        /// <summary>
        /// 在目标调用前运行拦截器逻辑
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        protected override void Before(IInvocationContext context) =>
            throw new InvalidOperationException("before-boom");
        /// <summary>
        /// 说明After在当前类型中的职责
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        protected override void After(IInvocationContext context) => Calls.Add("after");
        /// <summary>
        /// 说明On异常在当前类型中的职责
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <param name="exception">用于模拟异常流程的异常实例</param>
        protected override void OnException(IInvocationContext context, Exception exception) =>
            Calls.Add("onexception");
    }

    /// <summary>
    /// 验证Happy路径Runs前置处理继续处理After不带On异常
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task HappyPath_RunsBeforeProceedAfter_WithoutOnException()
    {
        var sut = new RecordingInterceptor();
        var context = new FakeInvocationContext();

        await sut.InterceptAsync(context);

        sut.Calls.Should().Equal("before", "after");
        context.ProceedCount.Should().Be(1);
    }

    /// <summary>
    /// 验证异常路径RunsOn异常ThenAfter和重新抛出
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证前置处理抛出异常不继续处理OrRunAfterOrOn异常和Propagates
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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
